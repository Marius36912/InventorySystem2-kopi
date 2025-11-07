// Data/DbReader.cs
using System.Linq;
using Microsoft.EntityFrameworkCore;
using InventorySystem2.Data.Entities;

namespace InventorySystem2.Data
{
    /// <summary>
    /// Henter data fra SQLite med EF Core.
    /// </summary>
    public static class DbReader
    {
        public static OrderBookEntity ReadOrderBook()
        {
            using var db = new InventoryDbContext();

            var book = db.OrderBooks
                .Include(o => o.QueuedOrders)
                .ThenInclude(ord => ord.OrderLines)
                .ThenInclude(ol => ol.Item)
                .Include(o => o.ProcessedOrders)
                .ThenInclude(ord => ord.OrderLines)
                .ThenInclude(ol => ol.Item)
                .FirstOrDefault();

            return book ?? new OrderBookEntity();
        }

        public static InventoryEntity ReadInventory()
        {
            using var db = new InventoryDbContext();

            var inv = db.Inventories
                .Include(i => i.Stock)
                .FirstOrDefault();

            return inv ?? new InventoryEntity { Id = "main", Stock = new() };
        }
    }
}