using Microsoft.EntityFrameworkCore;
using InventorySystem2.Data.Entities; // vores EF-entiteter

namespace InventorySystem2.Data
{
    /// <summary>
    /// EF Core DbContext for InventorySystem2 (SQLite).
    /// </summary>
    public sealed class InventoryDbContext : DbContext
    {
        // ===== DbSets (entities) =====
        public DbSet<ItemEntity> Items => Set<ItemEntity>();
        public DbSet<UnitItemEntity> UnitItems => Set<UnitItemEntity>();
        public DbSet<BulkItemEntity> BulkItems => Set<BulkItemEntity>();
        public DbSet<InventoryEntity> Inventories => Set<InventoryEntity>();
        public DbSet<OrderLineEntity> OrderLines => Set<OrderLineEntity>();
        public DbSet<OrderEntity> Orders => Set<OrderEntity>();
        public DbSet<OrderBookEntity> OrderBooks => Set<OrderBookEntity>();

        // ===== DB config =====
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=../../../inventory.sqlite");

        // ===== Model config (relationer + TPH-arv) =====
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Items arv (TPH)
            modelBuilder.Entity<ItemEntity>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<ItemEntity>("Item")
                .HasValue<UnitItemEntity>("UnitItem")
                .HasValue<BulkItemEntity>("BulkItem");

            // Inventory (1) -> Item (N)
            modelBuilder.Entity<InventoryEntity>()
                .HasMany(inv => inv.Stock)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            // Order (1) -> OrderLine (N)
            modelBuilder.Entity<OrderEntity>()
                .HasMany(o => o.OrderLines)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            // === Gør OrderBook-relations-FK'er eksplicit nullable (optionelle) ===
            // Skygge-FK'er som int? så en ordre kan være i enten Queue ELLER Processed
            modelBuilder.Entity<OrderEntity>().Property<int?>("QueuedOrderBookId");
            modelBuilder.Entity<OrderEntity>().Property<int?>("ProcessedOrderBookId");

            // OrderBook (1) -> Orders (N) for Queue (valgfri)
            modelBuilder.Entity<OrderBookEntity>()
                .HasMany(ob => ob.QueuedOrders)
                .WithOne()
                .HasForeignKey("QueuedOrderBookId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // OrderBook (1) -> Orders (N) for Processed (valgfri)
            modelBuilder.Entity<OrderBookEntity>()
                .HasMany(ob => ob.ProcessedOrders)
                .WithOne()
                .HasForeignKey("ProcessedOrderBookId")
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            base.OnModelCreating(modelBuilder);
        }
    }
}
