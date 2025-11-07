// Data/Entities.cs
using System;
using System.Collections.Generic;

namespace InventorySystem2.Data.Entities;

public class ItemEntity
{
    public string Id { get; set; } = null!;
    public decimal PricePerUnit { get; set; }
    public decimal Quantity { get; set; }
    public uint InventoryLocation { get; set; }
}

public class BulkItemEntity : ItemEntity
{
    public string MeasurementUnit { get; set; } = null!;
}

public class UnitItemEntity : ItemEntity
{
    public double Weight { get; set; }
}

public class InventoryEntity
{
    public string Id { get; set; } = null!;
    public List<ItemEntity> Stock { get; set; } = new();
}

public class OrderLineEntity
{
    public int Id { get; set; }
    public double Quantity { get; set; }
    public ItemEntity Item { get; set; } = null!;
}

public class OrderEntity
{
    public int Id { get; set; }
    public DateTime Time { get; set; }
    public List<OrderLineEntity> OrderLines { get; set; } = new();
}

public class OrderBookEntity
{
    public int Id { get; set; }
    public List<OrderEntity> QueuedOrders { get; set; } = new();
    public List<OrderEntity> ProcessedOrders { get; set; } = new();
}