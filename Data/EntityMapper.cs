// Data/EntityMapper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem2.Data.Entities;
using InventorySystem2.Models;

namespace InventorySystem2.Data
{
    /// <summary>
    /// Mapper mellem EF-entiteter og de eksisterende domæneklasser (ISv3.Models).
    /// Retning: DB -> domæne (read).
    /// </summary>
    public static class EntityMapper
    {
        public static Item MapItem(ItemEntity e)
        {
            return e switch
            {
                BulkItemEntity b => new BulkItem(b.Id, b.PricePerUnit, b.MeasurementUnit),
                UnitItemEntity u => new UnitItem(u.Id, u.PricePerUnit, u.Weight),
                _                => new Item(e.Id, e.PricePerUnit)
            };
        }

        public static Inventory MapInventory(InventoryEntity inv)
        {
            var dict = new Dictionary<Item, double>();
            foreach (var it in inv.Stock ?? new List<ItemEntity>())
            {
                var dItem = MapItem(it);
                dict[dItem] = (double)it.Quantity; // entitet har decimal, domænet bruger double
            }
            return new Inventory(dict);
        }

        public static OrderLine MapOrderLine(OrderLineEntity line)
            => new OrderLine(MapItem(line.Item), line.Quantity);

        public static Order MapOrder(OrderEntity o)
            => new Order(o.Time, (o.OrderLines ?? new List<OrderLineEntity>()).Select(MapOrderLine).ToList());

        public static OrderBook MapOrderBook(OrderBookEntity e)
        {
            var book = new OrderBook();
            foreach (var qo in e.QueuedOrders ?? new List<OrderEntity>())
                book.QueuedOrders.Add(MapOrder(qo));
            foreach (var po in e.ProcessedOrders ?? new List<OrderEntity>())
                book.ProcessedOrders.Add(MapOrder(po));
            return book;
        }
    }
}