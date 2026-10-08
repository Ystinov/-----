using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ПАБГН.Services;
using ПАБГН.ViewModels;

namespace ПАБГН.Views;

/// <summary>Главное окно приложения с меню разделов.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        viewModel.LogoutRequested += OnLogoutRequested;
    }

    /// <summary>Выход из системы: закрываем главное окно и показываем окно входа.</summary>
    private void OnLogoutRequested()
    {
        App.Services.GetRequiredService<AuthService>().Logout();
        App.Services.GetRequiredService<LoginWindow>().Show();
        Close();
    }
}
