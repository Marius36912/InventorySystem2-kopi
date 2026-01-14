using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using InventorySystem2.Auth;
using InventorySystem2.ViewModels;

namespace InventorySystem2.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void Logout_Click(object? sender, RoutedEventArgs e)
    {
        Session.CurrentUser = null;

        var login = new LoginWindow();
        login.Show();

        Close();
    }
    private async void CreateUser_Click(object? sender, RoutedEventArgs e)
    {
        // Sikkerhed: kun admin må åbne vinduet
        if (Session.CurrentUser?.IsAdmin != true)
            return;

        var w = new RegisterWindow();
        await w.ShowDialog(this);
    }
}