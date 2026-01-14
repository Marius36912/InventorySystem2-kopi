using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using InventorySystem2.Auth;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace InventorySystem2.Views;

public partial class LoginWindow : Window
{
    private TextBox UsernameBoxCtrl => this.FindControl<TextBox>("UsernameBox")!;
    private TextBox PasswordBoxCtrl => this.FindControl<TextBox>("PasswordBox")!;
    private TextBlock StatusTextCtrl => this.FindControl<TextBlock>("StatusText")!;


    private readonly AuthDbContext _db = new();
    private readonly AccountService _accounts;

    public LoginWindow()
    {
        InitializeComponent();
        _accounts = new AccountService(_db, new PasswordHasher());
        // Bootstrap admin, hvis DB er tom
        _ = EnsureBootstrapAdminAsync();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    private async void Login_Click(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBoxCtrl.Text ?? "";
        var password = PasswordBoxCtrl.Text ?? "";

        if (!await _accounts.UsernameExistsAsync(username))
        {
            StatusTextCtrl.Text = "Username does not exist.";
            return;
        }

        if (!await _accounts.CredentialsCorrectAsync(username, password))
        {
            StatusTextCtrl.Text = "Wrong password.";
            return;
        }

        var account = await _accounts.GetAccountAsync(username);
        Session.CurrentUser = new UserSession(account.Username, account.isAdmin);

        var main = new MainWindow();
        main.Show();
        Close();
    }
    private async Task EnsureBootstrapAdminAsync()
    {
        await _db.Database.EnsureCreatedAsync();

        // Hvis der allerede findes brugere, gør ingenting
        if (await _db.Accounts.AnyAsync())
            return;

        // Opret første admin (kun én gang)
        await _accounts.NewAccountAsync("admin", "admin", isAdmin: true);
        StatusTextCtrl.Text = "First run: admin/admin created ✅";
    }
}