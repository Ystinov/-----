using System.Windows;
using System.Windows.Controls;
using ПАБГН.ViewModels;

namespace ПАБГН.Views;

/// <summary>Раздел «Сотрудники» (только для администратора).</summary>
public partial class UsersView : UserControl
{
    public UsersView() => InitializeComponent();

    private UsersViewModel ViewModel => (UsersViewModel)DataContext;

    /// <summary>Пароль из PasswordBox нельзя связать, поэтому передаём его вручную.</summary>
    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        ViewModel.EditPassword = PasswordInput.Password;
        ViewModel.SaveCommand.Execute(null);
        PasswordInput.Clear();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        ViewModel.NewCommand.Execute(null);
        PasswordInput.Clear();
    }
}
