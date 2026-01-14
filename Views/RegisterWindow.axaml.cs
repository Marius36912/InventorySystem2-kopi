using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using InventorySystem2.Auth;

namespace InventorySystem2.Views;

public partial class RegisterWindow : Window
{
    private readonly AuthDbContext _db = new();
    private readonly AccountService _accounts;

    public RegisterWindow()
    {
        InitializeComponent();
        _accounts = new AccountService(_db, new PasswordHasher());
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private TextBox UsernameBoxCtrl => this.FindControl<TextBox>("UsernameBox")!;
    private TextBox PasswordBoxCtrl => this.FindControl<TextBox>("PasswordBox")!;
    
    private CheckBox IsAdminCheckCtrl => this.FindControl<CheckBox>("IsAdminCheck")!;
    private TextBlock StatusTextCtrl => this.FindControl<TextBlock>("StatusText")!;

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close();

    private async void Create_Click(object? sender, RoutedEventArgs e)
    {
        // ekstra sikkerhed: kun admin kan oprette
        if (Session.CurrentUser?.IsAdmin != true)
        {
            StatusTextCtrl.Text = "Only admin can create users.";
            return;
        }

        var username = UsernameBoxCtrl.Text?.Trim() ?? "";
        var password = PasswordBoxCtrl.Text ?? "";

        if (username.Length < 3)
        {
            StatusTextCtrl.Text = "Username must be at least 3 characters.";
            return;
        }

        if (password.Length < 4)
        {
            StatusTextCtrl.Text = "Password must be at least 4 characters.";
            return;
        }

        await _db.Database.EnsureCreatedAsync();

        if (await _accounts.UsernameExistsAsync(username))
        {
            StatusTextCtrl.Text = "Username is already taken.";
            return;
        }
       
        var makeAdmin = IsAdminCheckCtrl.IsChecked == true;
        
        await _accounts.NewAccountAsync(username, password, isAdmin: makeAdmin);
        StatusTextCtrl.Text = "User created ✅";
    }
}