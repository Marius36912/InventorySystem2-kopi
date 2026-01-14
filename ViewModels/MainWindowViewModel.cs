using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using InventorySystem2.Models;
using InventorySystem2.Data;
using InventorySystem2.Data.Entities;
using InventorySystem2.Auth;

namespace InventorySystem2.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly Inventory _inventory;   // lager (domæne)
    private readonly OrderBook _orderBook;   // ordrebog (domæne)

    // ========= Robot settings =========
    private string _robotIp = "localhost";
    public string RobotIp
    {
        get => _robotIp;
        set
        {
            if (_robotIp == value) return;
            _robotIp = value;
            OnPropertyChanged();
            _robot = null; // force reconnect når IP ændres
        }
    }

    private const int RobotPort = 30002;
    private Robot? _robot;

    private void EnsureRobot()
    {
        _robot ??= new Robot(RobotIp, RobotPort);
    }
    // ================================

    // ========= Optional DB safety toggles (fra din "større" version) =========
    // Shadow-FK + WAL checkpoint hjælper typisk når EF ikke flytter relationen korrekt i Orders-tabellen.
    private const bool UseShadowFkFix = true;
    private const bool UseWalCheckpoint = true;
    // =======================================================================

    public ObservableCollection<Order> QueuedOrders { get; }
    public ObservableCollection<Order> ProcessedOrders { get; }

    private decimal _totalRevenue;
    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set { _totalRevenue = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }
    
    public bool IsAdmin => Session.CurrentUser?.IsAdmin == true;
    public ICommand ProcessNextCommand { get; }
    public ICommand PingCommand { get; }
    public ICommand CheckDbCommand { get; }
    public ICommand ResetDbCommand { get; }
    

    // ===== UI: Create order =====
    public ObservableCollection<string> AvailableItemIds { get; } = new();

    private string? _selectedItemId;
    public string? SelectedItemId
    {
        get => _selectedItemId;
        set { _selectedItemId = value; OnPropertyChanged(); }
    }

    private string _newQuantityText = "1";
    public string NewQuantityText
    {
        get => _newQuantityText;
        set { _newQuantityText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<DraftOrderLine> DraftOrderLines { get; } = new();

    public ICommand AddOrderLineCommand { get; }
    public ICommand RemoveOrderLineCommand { get; }
    public ICommand SubmitOrderCommand { get; }

    public MainWindowViewModel()
    {
        // === Læs fra database ===
        var bookEntity = DbReader.ReadOrderBook();
        var invEntity = DbReader.ReadInventory();
        // ===== UI: Create order dropdown (White/Black fra inventory) =====
        AvailableItemIds.Clear();
        foreach (var it in invEntity.Stock)
            AvailableItemIds.Add(it.Id);

        SelectedItemId = AvailableItemIds.FirstOrDefault();
        NewQuantityText = "1";

        _orderBook = MapOrderBook(bookEntity);
        _inventory = MapInventory(invEntity);

        QueuedOrders = new ObservableCollection<Order>(_orderBook.QueuedOrders);
        ProcessedOrders = new ObservableCollection<Order>(_orderBook.ProcessedOrders);
        TotalRevenue = _orderBook.TotalRevenue();

        ProcessNextCommand = new RelayCommandAsync(_ => ProcessNextAsync(),
                                                   _ => QueuedOrders.Count > 0);
        PingCommand = new RelayCommandAsync(_ => PingRobotAsync());
        CheckDbCommand = new RelayCommandAsync(_ => CheckDbAsync());
        ResetDbCommand = new RelayCommandAsync(_ => ResetDbAsync());

        QueuedOrders.CollectionChanged += (_, __)
            => ((RelayCommandAsync)ProcessNextCommand).RaiseCanExecuteChanged();
        AddOrderLineCommand = new RelayCommandAsync(_ => AddOrderLineAsync());
        RemoveOrderLineCommand = new RelayCommandAsync(line => RemoveOrderLineAsync(line));
        SubmitOrderCommand = new RelayCommandAsync(_ => SubmitOrderAsync(), _ => DraftOrderLines.Count > 0);

        DraftOrderLines.CollectionChanged += (_, __) =>
            ((RelayCommandAsync)SubmitOrderCommand).RaiseCanExecuteChanged();

    }
    
    // ========== Mapping: Entities -> Domæne ==========
    private static Item MapItem(ItemEntity e) =>
        e switch
        {
            BulkItemEntity b => new BulkItem(b.Id, b.PricePerUnit, b.MeasurementUnit),
            UnitItemEntity u => new UnitItem(u.Id, u.PricePerUnit, u.Weight),
            _ => new Item(e.Id, e.PricePerUnit)
        };

    private static Inventory MapInventory(InventoryEntity inv)
    {
        var dict = new Dictionary<Item, double>();
        foreach (var it in inv.Stock ?? new List<ItemEntity>())
            dict[MapItem(it)] = (double)it.Quantity;
        return new Inventory(dict);
    }

    private static OrderLine MapOrderLine(OrderLineEntity line)
        => new OrderLine(MapItem(line.Item), line.Quantity);

    private static Order MapOrder(OrderEntity o)
        => new Order(o.Time, (o.OrderLines ?? new List<OrderLineEntity>()).Select(MapOrderLine).ToList());

    private static OrderBook MapOrderBook(OrderBookEntity e)
    {
        var book = new OrderBook();
        foreach (var qo in e.QueuedOrders ?? new List<OrderEntity>())
            book.QueuedOrders.Add(MapOrder(qo));
        foreach (var po in e.ProcessedOrders ?? new List<OrderEntity>())
            book.ProcessedOrders.Add(MapOrder(po));
        return book;
    }
    private void ReloadUiFromDb()
    {
        var bookEntity = DbReader.ReadOrderBook();
        var freshBook = MapOrderBook(bookEntity);

        // altid stabil sortering
        var queuedSorted = freshBook.QueuedOrders.OrderBy(o => o.Time).ToList();
        var processedSorted = freshBook.ProcessedOrders.OrderBy(o => o.Time).ToList();

        QueuedOrders.Clear();
        foreach (var o in queuedSorted) QueuedOrders.Add(o);

        ProcessedOrders.Clear();
        foreach (var o in processedSorted) ProcessedOrders.Add(o);

        TotalRevenue = freshBook.TotalRevenue();
    }

    // "Ping robot"
    public async Task PingRobotAsync()
    {
        try
        {
            EnsureRobot();

            // hvis du vil bruge dashboard (29999) senere:
            // _robot!.ReleaseBrakes();

            StatusMessage = $"Robot OK ✅ ({RobotIp}:{RobotPort})";
            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Robot error: {ex.Message}";
        }
    }

    private async Task CheckDbAsync()
    {
        try
        {
            using var db = new InventoryDbContext();
            var can = await db.Database.CanConnectAsync();
            var path = db.Database.GetDbConnection().DataSource;

            StatusMessage = can
                ? $"DB OK ✅  Path: {path}"
                : $"DB not reachable ❌  Path: {path}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB error: {ex.Message}";
        }
    }

    private async Task ResetDbAsync()
    {
        try
        {
            StatusMessage = "Resetting DB…";
            await Task.Run(() => DbSeeder.ResetToSeed());

            // Reload UI fra DB (single source of truth)
            ReloadUiFromDb();
            StatusMessage = "DB reset OK ✅";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reset error: {ex.Message}";
        }
    }
    private Task AddOrderLineAsync()
{
    if (string.IsNullOrWhiteSpace(SelectedItemId))
    {
        StatusMessage = "Vælg et produkt først.";
        return Task.CompletedTask;
    }

    if (!int.TryParse(NewQuantityText, out var qty) || qty < 1)
    {
        StatusMessage = "Antal skal være et helt tal >= 1.";
        return Task.CompletedTask;
    }

    // Merge: hvis samme item findes, læg quantity sammen
    var existing = DraftOrderLines.FirstOrDefault(d => d.ItemId == SelectedItemId);
    if (existing is null)
    {
        DraftOrderLines.Add(new DraftOrderLine(SelectedItemId, qty));
    }
    else
    {
        existing.Add(qty);

        // Force refresh af Display (simpelt og stabilt)
        var idx = DraftOrderLines.IndexOf(existing);
        DraftOrderLines.RemoveAt(idx);
        DraftOrderLines.Insert(idx, existing);
    }

    StatusMessage = "Order line added.";
    return Task.CompletedTask;
}

private Task RemoveOrderLineAsync(object? lineObj)
{
    if (lineObj is DraftOrderLine line)
        DraftOrderLines.Remove(line);

    return Task.CompletedTask;
}

private async Task SubmitOrderAsync()
{
    try
    {
        if (DraftOrderLines.Count == 0)
        {
            StatusMessage = "Ingen orderlines at sende.";
            return;
        }

        using var db = new InventoryDbContext();

        var book = await db.OrderBooks
            .Include(ob => ob.QueuedOrders)
            .FirstOrDefaultAsync();

        if (book is null)
        {
            StatusMessage = "DB error: OrderBook not found.";
            return;
        }

        // Hent items fra DB (så OrderLines peger på eksisterende rows)
        var ids = DraftOrderLines.Select(d => d.ItemId).ToList();
        var items = await db.Items.Where(i => ids.Contains(i.Id)).ToListAsync();

        var order = new OrderEntity
        {
            Time = DateTime.Now,
            OrderLines = DraftOrderLines.Select(d =>
            {
                var item = items.First(i => i.Id == d.ItemId);
                return new OrderLineEntity { Item = item, Quantity = d.Quantity };
            }).ToList()
        };

        book.QueuedOrders.Add(order);

        await db.SaveChangesAsync();

        DraftOrderLines.Clear();
        ReloadUiFromDb();

        StatusMessage = "Order submitted ✅";
    }
    catch (Exception ex)
    {
        StatusMessage = $"Submit error: {ex.Message}";
    }
}

    // Process next: ROBOT + DB update
    private async Task ProcessNextAsync()
    {
        try
        {
            using var db = new InventoryDbContext();

            var book = await db.OrderBooks
                .Include(o => o.QueuedOrders)
                    .ThenInclude(q => q.OrderLines)
                        .ThenInclude(l => l.Item)
                .Include(o => o.ProcessedOrders)
                    .ThenInclude(p => p.OrderLines)
                        .ThenInclude(l => l.Item)
                .FirstOrDefaultAsync();

            if (book is null)
            {
                StatusMessage = "DB warning: OrderBook not found.";
                return;
            }

            var next = book.QueuedOrders
                .OrderBy(o => o.Time)
                .FirstOrDefault();

            if (next is null)
            {
                StatusMessage = "DB info: No queued order to move.";
                return;
            }

            // --- ROBOT: kør URScript ---
            EnsureRobot();
            bool sim = RobotIp.Trim().Equals("localhost", StringComparison.OrdinalIgnoreCase);

            foreach (var line in next.OrderLines)
            {
                if (line.Item is null) continue;

                // Quantity -> repeats (loop håndteres inde i URScript)
                int repeat = (int)line.Quantity;
                if (repeat < 1 || Math.Abs(line.Quantity - repeat) > 0.0001)
                {
                    StatusMessage = $"Invalid quantity: {line.Quantity}. Must be a whole number.";
                    return;
                }
                
                string program =
                        line.Item.Id.Equals("Black Shell", StringComparison.OrdinalIgnoreCase)
                            ? RobotPositions.ItemSorter_BlackShell(sim, repeat)
                            : RobotPositions.ItemSorter_WhiteShell(sim, repeat);

                _robot!.SendProgram(program, 1000);
                await Task.Delay(150);
            }

            // --- DB: opdater quantities + flyt order ---
            foreach (var line in next.OrderLines)
                if (line.Item is not null)
                    line.Item.Quantity -= (decimal)line.Quantity;

            book.QueuedOrders.Remove(next);
            book.ProcessedOrders.Add(next);

            if (UseShadowFkFix)
                TrySetShadowOrderBookFks(db, next, book.Id);

            await db.SaveChangesAsync();

            if (UseWalCheckpoint)
                await db.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");

            StatusMessage = "DB updated ✅ + Robot ran ✅";
            
            // Reload UI fra DB så UI matcher præcis det DB gjorde
            ReloadUiFromDb();

        }
        catch (Exception ex)
        {
            StatusMessage = $"DB save/robot error: {ex.Message}";
        }
    }
    
    /// Sikker version af din shadow-FK workaround:
    /// Kører kun hvis EF faktisk har de shadow properties på OrderEntity.
    private static void TrySetShadowOrderBookFks(DbContext db, OrderEntity order, int processedBookId)
    {
        try
        {
            var entry = db.Entry(order);

            // Kun sæt hvis props findes (ellers kaster EF)
            var hasQueued = entry.Metadata.FindProperty("QueuedOrderBookId") is not null;
            var hasProcessed = entry.Metadata.FindProperty("ProcessedOrderBookId") is not null;

            if (hasQueued)
                entry.Property<int?>("QueuedOrderBookId").CurrentValue = null;

            if (hasProcessed)
                entry.Property<int?>("ProcessedOrderBookId").CurrentValue = processedBookId;
        }
        catch
        {
            // Ignorer: hvis modellen ikke bruger shadow props i denne version af DB/EF,
            // så er workaround ikke nødvendig.
        }
    }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
public sealed class DraftOrderLine
{
    public string ItemId { get; }
    public int Quantity { get; private set; }

    public DraftOrderLine(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }

    public void Add(int qty) => Quantity += qty;

    public string Display => $"{ItemId} x {Quantity}";
}

// Async ICommand helper
public sealed class RelayCommandAsync : ICommand
{
    private readonly Func<object?, Task> _executeAsync;
    private readonly Func<object?, bool>? _canExecute;
    private bool _isExecuting;

    public RelayCommandAsync(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
    {
        _executeAsync = executeAsync;
        _canExecute = canExecute;
    }
    
    
    public bool CanExecute(object? parameter)
        => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try
        {
            _isExecuting = true;
            RaiseCanExecuteChanged();
            await _executeAsync(parameter);
        }
        finally
        {
            _isExecuting = false;
            RaiseCanExecuteChanged();
        }
    }

    public event EventHandler? CanExecuteChanged;
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
