using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ПАБГН.ViewModels;

namespace ПАБГН.Views;

/// <summary>Окно входа в систему.</summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => LoginBox.Focus();
    }

    private void OnLoginClick(object sender, RoutedEventArgs e) => TryLogin();

    private void OnPasswordKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryLogin();
    }

    private void TryLogin() => _viewModel.TryLogin(PasswordInput.Password);

    /// <summary>После успешного входа открываем главное окно и закрываем окно входа.</summary>
    private void OnLoginSucceeded(Models.User user)
    {
        var main = App.Services.GetRequiredService<MainWindow>();
        main.Show();
        Close();
    }
}
