// Data/DbSeeder.cs
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;              // ClearAllPools(); (beholdt for good measure)
using Microsoft.EntityFrameworkCore;
using InventorySystem2.Data.Entities;

namespace InventorySystem2.Data
{
    /// <summary>
    /// Opretter databasefilen og indsætter et lille startdatasæt én gang.
    /// Kan også nulstille via ResetToSeed() UDEN at slette .sqlite-filen (undgår OneDrive locks).
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// Laver DB + tabeller hvis de ikke findes og seed’er første gang.
        /// No-op hvis der allerede er data.
        /// </summary>
        public static void EnsureSeedData()
        {
            using var db = new InventoryDbContext();
            db.Database.EnsureCreated();

            // Hvis der allerede er data, gør ingenting
            if (db.OrderBooks.Any() || db.Items.Any())
                return;

            // ----- Items -----
            var rice  = new BulkItemEntity { Id = "Rice",  PricePerUnit = 1.20m, Quantity = 12m,  MeasurementUnit = "kg", InventoryLocation = 1 };
            var cable = new BulkItemEntity { Id = "Cable", PricePerUnit = 4.00m, Quantity = 40m, MeasurementUnit = "m",  InventoryLocation = 2 };
            var screw = new UnitItemEntity { Id = "Screw", PricePerUnit = 0.50m, Quantity = 50m, Weight = 0.02, InventoryLocation = 3 };
            var pen   = new UnitItemEntity { Id = "Pen",   PricePerUnit = 2.00m, Quantity = 3m,  Weight = 0.01, InventoryLocation = 4 };

            // ----- Inventory -----
            var inv = new InventoryEntity
            {
                Id = "main",
                Stock = new List<ItemEntity> { rice, cable, screw, pen }
            };

            // ----- Tre ordrer i kø (eksempeldata) -----
            var o1 = new OrderEntity
            {
                Time = DateTime.Now.AddMinutes(-10),
                OrderLines = new List<OrderLineEntity>
                {
                    new() { Item = rice,  Quantity = 2.5 },
                    new() { Item = pen,   Quantity = 2   },
                }
            };

            var o2 = new OrderEntity
            {
                Time = DateTime.Now.AddMinutes(-7),
                OrderLines = new List<OrderLineEntity>
                {
                    new() { Item = screw, Quantity = 10  },
                    new() { Item = cable, Quantity = 3   },
                }
            };

            var o3 = new OrderEntity
            {
                Time = DateTime.Now.AddMinutes(-2),
                OrderLines = new List<OrderLineEntity>
                {
                    new() { Item = pen,   Quantity = 1   },
                    new() { Item = rice,  Quantity = 1.0 },
                }
            };

            var book = new OrderBookEntity
            {
                QueuedOrders = new List<OrderEntity> { o1, o2, o3 },
                ProcessedOrders = new List<OrderEntity>()
            };

            // ----- Gem alt -----
            db.Add(inv);
            db.Add(book);
            db.SaveChanges();
        }

        /// <summary>
        /// Nulstil database-INDHOLD til seed-tilstand UDEN at slette .sqlite-filen.
        /// Vi tømmer tabeller i korrekt rækkefølge (respekterer FK'er), resetter identity,
        /// og seed'er igen. Dette undgår filsystem-låsning i OneDrive.
        /// </summary>
        public static void ResetToSeed()
        {
            // Frigiv evt. hængende handles/pools (hjælper ved hurtige tryk)
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SqliteConnection.ClearAllPools();

            using var db = new InventoryDbContext();
            db.Database.EnsureCreated();

            // Kør alt i én transaktion
            using var tx = db.Database.BeginTransaction();

            // Giv SQLite lidt mere tålmodighed hvis OneDrive låser kort
            db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=1500;");
            // Midlertidigt slå FK-check fra mens vi tømmer i rækkefølge
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=OFF;");

            // Navnene her matcher jeres faktiske tabeller (lowercase plural),
            // jf. det du listede: inventories, items, orderbooks, orderlines, orders.
            // Rækkefølge: afhængigheder først (orderlines -> orders -> orderbooks -> items -> inventories)
            db.Database.ExecuteSqlRaw("DELETE FROM orderlines;");
            db.Database.ExecuteSqlRaw("DELETE FROM orders;");
            db.Database.ExecuteSqlRaw("DELETE FROM orderbooks;");
            db.Database.ExecuteSqlRaw("DELETE FROM items;");
            db.Database.ExecuteSqlRaw("DELETE FROM inventories;");

            // Nulstil autoincrement tællere (hvis nogle tabeller bruger INTEGER PK)
            db.Database.ExecuteSqlRaw("DELETE FROM sqlite_sequence;");

            // Slå FK-check til igen
            db.Database.ExecuteSqlRaw("PRAGMA foreign_keys=ON;");

            tx.Commit();

            // Rens change-tracker så vi ikke sidder med ‘ghost’ entities
            db.ChangeTracker.Clear();

            // Seed nyt sæt
            EnsureSeedData();
        }
    }
}
