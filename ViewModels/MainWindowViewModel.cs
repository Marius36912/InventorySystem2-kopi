using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.EntityFrameworkCore;        // til Include/ThenInclude i save-koden
using InventorySystem2.Models;             // dine domæneklasser til UI
using InventorySystem2.Data;               // DbReader + DbContext + DbSeeder
using InventorySystem2.Data.Entities;      // entity-typer til mapping

namespace InventorySystem2.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly Inventory _inventory;   // lager (domæne)
    private readonly OrderBook _orderBook;   // ordrebog (domæne)
    private readonly Robot _robot = new Robot("localhost", 30002); // robot socket

    public ObservableCollection<Order> QueuedOrders  { get; }     // kø
    public ObservableCollection<Order> ProcessedOrders { get; }   // færdige

    private decimal _totalRevenue;
    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set { _totalRevenue = value; OnPropertyChanged(); }
    }

    // statuslinje-tekst
    private string _statusMessage = "Ready";
    public string StatusMessage
    {
        get => _statusMessage;
        private set { _statusMessage = value; OnPropertyChanged(); }
    }

    public ICommand ProcessNextCommand { get; }
    public ICommand PingCommand { get; }             // Ping/vinke
    public ICommand CheckDbCommand { get; }          // “Check connection”
    public ICommand ResetDbCommand { get; }          // NY: Nulstil database til seed

    public MainWindowViewModel()
    {
        // === Læs fra database ===
        var bookEntity = DbReader.ReadOrderBook();
        var invEntity  = DbReader.ReadInventory();

        _orderBook = MapOrderBook(bookEntity);   // entities -> domæne
        _inventory = MapInventory(invEntity);

        // bind til UI
        QueuedOrders    = new ObservableCollection<Order>(_orderBook.QueuedOrders);
        ProcessedOrders = new ObservableCollection<Order>(_orderBook.ProcessedOrders);
        TotalRevenue    = _orderBook.TotalRevenue();

        // knapper (async)
        ProcessNextCommand = new RelayCommandAsync(_ => ProcessNextAsync(),
                                                   _ => QueuedOrders.Count > 0);
        PingCommand        = new RelayCommandAsync(_ => PingRobotAsync());
        CheckDbCommand     = new RelayCommandAsync(_ => CheckDbAsync());
        ResetDbCommand     = new RelayCommandAsync(_ => ResetDbAsync());  // NY

        // enable/disable når kø ændres
        QueuedOrders.CollectionChanged += (_, __)
            => ((RelayCommandAsync)ProcessNextCommand).RaiseCanExecuteChanged();
    }

    // ========== Mapping: Entities -> Domæne ==========
    private static Item MapItem(ItemEntity e) =>
        e switch
        {
            BulkItemEntity b => new BulkItem(b.Id, b.PricePerUnit, b.MeasurementUnit),
            UnitItemEntity u => new UnitItem(u.Id, u.PricePerUnit, u.Weight),
            _                => new Item(e.Id, e.PricePerUnit)
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

    // flyt fra slot -> S (simpel generator + lille pause)
    private async Task PickToS(string slot, uint itemId)
    {
        var from = slot.ToLowerInvariant() switch
        {
            "a" => RobotPositions.A,
            "b" => RobotPositions.B,
            "c" => RobotPositions.C,
            _   => RobotPositions.A
        };
        var to = RobotPositions.S;

        var program = RobotPositions.GenerateMove(from, to);
        _robot.SendProgram(program, itemId);

        await Task.Delay(300);
    }

    // "Ping robot" – tydelig vinke-bevægelse 3 gange
    public async Task PingRobotAsync()
    {
        var prog = @"
def ping():
  home = [0, -1.57, 0, -1.57, 0, 0]
  left =  p[0.25, -0.25, 0.20, 0, -3.1415, 0]
  right = p[0.25,  0.25, 0.20, 0, -3.1415, 0]
  movej(home, a=1.2, v=0.6)
  i = 0
  while (i < 3):
    movej(get_inverse_kin(left),  a=1.2, v=0.6)
    movej(get_inverse_kin(right), a=1.2, v=0.6)
    i = i + 1
  end
end
";
        _robot.SendProgram(prog, 999);
        await Task.Delay(300);
    }

    // check DB-connection
    private async Task CheckDbAsync()
    {
        try
        {
            using var db = new InventorySystem2.Data.InventoryDbContext();
            var can = await db.Database.CanConnectAsync();
            StatusMessage = can ? "DB OK ✅" : "DB not reachable ❌";
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB error: {ex.Message}";
        }
    }

    // NY: reset DB til seed-tilstand og reload GUI
    private async Task ResetDbAsync()
    {
        try
        {
            StatusMessage = "Resetting DB…";
            await Task.Run(() => DbSeeder.ResetToSeed()); // delete + reseed

            // reload entities
            var bookEntity = DbReader.ReadOrderBook();
            var invEntity  = DbReader.ReadInventory();

            // map til domæne
            var freshBook = MapOrderBook(bookEntity);
            var freshInv  = MapInventory(invEntity);

            // opdater backing felter
            // (vi holder felterne readonly i signaturen og opdaterer kun GUI-collections)
            // Tøm og fyld collections så bindings ikke brydes
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

    // ordre -> robot (3 hop) -> flyt i UI -> opdater revenue -> GEM I DB
    private async Task ProcessNextAsync()
    {
        var processed = _orderBook.ProcessNextOrder(_inventory);
        if (processed is null) return;

        // === Robotdel: 3 hop fra a,b,c -> S ===
        await PickToS("a", 101);
        await PickToS("b", 102);
        await PickToS("c", 103);

        Console.WriteLine("Shipment box moved by conveyor belt.");

        // GUI-opdatering som før
        if (QueuedOrders.Count > 0) QueuedOrders.RemoveAt(0);
        ProcessedOrders.Add(processed);
        TotalRevenue = _orderBook.TotalRevenue();

        // === Persistér ændringen i databasen ===
        try
        {
            using var db = new InventoryDbContext();

            // Hent OrderBook + alle relaterede data
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

            // Find næste ordre i DB-køen (samme logik som i VM: den tidligste)
            var next = book.QueuedOrders
                .OrderBy(o => o.Time)
                .FirstOrDefault();

            if (next is null)
            {
                StatusMessage = "DB info: No queued order to move.";
                return;
            }

            // Opdater lager-mængder i DB for hvert order line
            foreach (var line in next.OrderLines)
                if (line.Item is not null)
                    line.Item.Quantity -= (decimal)line.Quantity;

            // Flyt ordren i DB: fra Queue -> Processed
            book.QueuedOrders.Remove(next);
            book.ProcessedOrders.Add(next);

            await db.SaveChangesAsync();
            StatusMessage = "DB updated ✅";
        }
        catch (Exception ex)
        {
            StatusMessage = $"DB save error: {ex.Message}";
        }
    }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// Async ICommand helper (uændret)
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
