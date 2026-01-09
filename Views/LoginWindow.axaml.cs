using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using InventorySystem2.Auth;

namespace InventorySystem2.Views;

public partial class LoginWindow : Window
{
    private TextBox UsernameBox => this.FindControl<TextBox>("UsernameBox");
    private TextBox PasswordBox => this.FindControl<TextBox>("PasswordBox");
    private TextBlock StatusText => this.FindControl<TextBlock>("StatusText");

    private readonly AuthDbContext _db = new();
    private readonly AccountService _accounts;

    public LoginWindow()
    {
        InitializeComponent();
        _accounts = new AccountService(_db, new PasswordHasher());
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void Seed_Click(object? sender, RoutedEventArgs e)
    {
        var created = await _db.Database.EnsureCreatedAsync();
        if (created)
        {
            await _accounts.NewAccountAsync("admin", "admin", true);
            await _accounts.NewAccountAsync("user", "user", false);
            StatusText.Text = "Created demo users: admin/admin and user/user";
        }
        else
        {
            StatusText.Text = "Auth database already exists.";
        }
    }

    private async void Login_Click(object? sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text ?? "";
        var password = PasswordBox.Text ?? "";

        if (!await _accounts.UsernameExistsAsync(username))
        {
            StatusText.Text = "Username does not exist.";
            return;
        }

        if (!await _accounts.CredentialsCorrectAsync(username, password))
        {
            StatusText.Text = "Wrong password.";
            return;
        }

        var account = await _accounts.GetAccountAsync(username);
        Session.CurrentUser = new UserSession(account.Username, account.isAdmin);

        var main = new MainWindow();
        main.Show();
        Close();
    }
}