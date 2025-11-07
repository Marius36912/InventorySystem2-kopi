using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Data.Sqlite;

namespace InventorySystem2
{
    public partial class App : Application
    {
        public override void Initialize() => AvaloniaXamlLoader.Load(this);

        public override void OnFrameworkInitializationCompleted()
        {
            // --- Robust seed med retry mod korte OneDrive-locks (disk I/O error 10) ---
            int attempts = 0;
            while (true)
            {
                try
                {
                    Data.DbSeeder.EnsureSeedData();
                    break;
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 10) // disk I/O error
                {
                    attempts++;

                    // Frigiv evt. hængende handles/pools og vent kort
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    SqliteConnection.ClearAllPools();

                    if (attempts >= 6) // ca. 1.2 sek max (200+400+...+1200 ms)
                        throw;

                    Thread.Sleep(200 * attempts);
                }
            }

            // --- Normal Avalonia-boot ---
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new Views.MainWindow
                {
                    DataContext = new ViewModels.MainWindowViewModel()
                };
            }

            // --- Ryd SQLite-pools ved proces-exit (sikrer fil-locks slippes) ---
            AppDomain.CurrentDomain.ProcessExit += (_, __) =>
            {
                try { SqliteConnection.ClearAllPools(); } catch { /* ignore */ }
            };

            base.OnFrameworkInitializationCompleted();
        }
    }
}