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

namespace InventorySystem2.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly Inventory _inventory;   // lager (domæne)
    private readonly OrderBook _orderBook;   // ordrebog (domæne)

    // ========= Robot settings (NY) =========
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
    // =======================================

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

    public ICommand ProcessNextCommand { get; }
    public ICommand PingCommand { get; }
    public ICommand CheckDbCommand { get; }
    public ICommand ResetDbCommand { get; }

    public MainWindowViewModel()
    {
        // === Læs fra database ===
        var bookEntity = DbReader.ReadOrderBook();
        var invEntity = DbReader.ReadInventory();

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
    // ================================================

    // "Ping robot" – bruger den nye RobotPositions.Wave()
    public async Task PingRobotAsync()
    {
        try
        {
            EnsureRobot();

            // hvis du vil bruge dashboard (29999) senere:
            // _robot!.ReleaseBrakes();

            _robot!.SendProgram(RobotPositions.Wave(), 999);
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
            StatusMessage = can ? "DB OK ✅" : "DB not reachable ❌";
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

            var bookEntity = DbReader.ReadOrderBook();
            var invEntity = DbReader.ReadInventory();

            var freshBook = MapOrderBook(bookEntity);

            QueuedOrders.Clear();
            foreach (var o in freshBook.QueuedOrders) QueuedOrders.Add(o);

            ProcessedOrders.Clear();
            foreach (var o in freshBook.ProcessedOrders) ProcessedOrders.Add(o);

            TotalRevenue = freshBook.TotalRevenue();
            StatusMessage = "DB reset OK ✅";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reset error: {ex.Message}";
        }
    }

    // ===============================
    // Process next: ROBOT + DB update
    // ===============================
    private async Task ProcessNextAsync()
    {
        // 1) Opdater domæne + UI (som før)
        var processed = _orderBook.ProcessNextOrder(_inventory);
        if (processed is null) return;

        if (QueuedOrders.Count > 0) QueuedOrders.RemoveAt(0);
        ProcessedOrders.Add(processed);
        TotalRevenue = _orderBook.TotalRevenue();

        // 2) Persistér + kør robot baseret på DB InventoryLocation
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

            // --- ROBOT: kør URScript ud fra locations ---
            EnsureRobot();

            foreach (var line in next.OrderLines)
            {
                if (line.Item is null) continue;

                // Pick fra item.InventoryLocation -> Assembly
                var program = RobotPositions.PickToAssembly(line.Item.InventoryLocation, fromStackIndex: 0);
                _robot!.SendProgram(program, 1000); // itemId bruges kun til navngivning/log

                await Task.Delay(150); // lille buffer mellem scripts
            }

            // Flyt færdig klods/produkt fra Assembly -> Output (valgfrit)
            _robot!.SendProgram(RobotPositions.AssemblyToOutput(), 2000);

            // --- DB: opdater quantities + flyt order ---
            foreach (var line in next.OrderLines)
                if (line.Item is not null)
                    line.Item.Quantity -= (decimal)line.Quantity;

            book.QueuedOrders.Remove(next);
            book.ProcessedOrders.Add(next);

            await db.SaveChangesAsync();
            StatusMessage = "DB updated ✅ + Robot ran ✅";
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB save/robot error: {ex.Message}";
        }
    }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
