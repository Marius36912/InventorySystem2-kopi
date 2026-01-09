using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem2.Auth;

public class AuthDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();

    private readonly string _dbPath;

    public AuthDbContext(string? dbPath = null)
    {
        _dbPath = dbPath ?? Path.Combine(AppContext.BaseDirectory, "auth.sqlite");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={_dbPath}");
}