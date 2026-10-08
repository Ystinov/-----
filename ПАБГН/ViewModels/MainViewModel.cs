using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Модель представления главного окна: меню разделов и текущий раздел.</summary>
public class MainViewModel : ViewModelBase
{
    /// <summary>Пункт меню: заголовок и соответствующая ему модель представления.</summary>
    public record NavigationItem(string Title, string Icon, ViewModelBase ViewModel);

    private readonly AuthService _auth;

    private NavigationItem? _selectedItem;
    private ViewModelBase? _currentViewModel;

    public MainViewModel(AuthService auth,
                         RoomsViewModel rooms,
                         GuestsViewModel guests,
                         BookingsViewModel bookings,
                         NewBookingViewModel newBooking,
                         PaymentsViewModel payments,
                         ServicesViewModel services,
                         ReportsViewModel reports,
                         UsersViewModel users)
    {
        _auth = auth;

        Items =
        [
            new NavigationItem("Новая бронь", "➕", newBooking),
            new NavigationItem("Брони", "📋", bookings),
            new NavigationItem("Номера", "🛏", rooms),
            new NavigationItem("Гости", "👤", guests),
            new NavigationItem("Оплата", "💳", payments),
            new NavigationItem("Услуги", "🍽", services),
            new NavigationItem("Отчёты", "📊", reports)
        ];

        // Раздел «Сотрудники» доступен только администратору
        if (auth.CurrentUser?.IsAdministrator == true)
            Items.Add(new NavigationItem("Сотрудники", "👥", users));

        // Переходы между разделами по действиям пользователя
        bookings.OpenPaymentsRequested = booking =>
        {
            payments.Open(booking);
            SelectedItem = Items.First(i => i.ViewModel == payments);
        };

        bookings.NavigateToNewBooking = () => SelectedItem = Items.First(i => i.ViewModel == newBooking);
        newBooking.BookingCreated += _ => SelectedItem = Items.First(i => i.ViewModel == bookings);

        LogoutCommand = new RelayCommand(_ => LogoutRequested?.Invoke());

        SelectedItem = Items[0];
    }

    /// <summary>Пункты меню (зависят от роли пользователя).</summary>
    public ObservableCollection<NavigationItem> Items { get; }

    /// <summary>Выбранный пункт меню. Переключение раздела обновляет его данные.</summary>
    public NavigationItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!SetProperty(ref _selectedItem, value))
                return;

            CurrentViewModel = value?.ViewModel;
            (value?.ViewModel as IRefreshable)?.Refresh();
        }
    }

    /// <summary>Модель представления отображаемого раздела.</summary>
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set => SetProperty(ref _currentViewModel, value);
    }

    public string UserName => _auth.CurrentUser?.FullName ?? string.Empty;

    public string UserRoleText => _auth.CurrentUser?.Role switch
    {
        UserRole.Administrator => "Администратор",
        UserRole.Manager => "Менеджер",
        UserRole.Housekeeper => "Горничная",
        _ => string.Empty
    };

    public ICommand LogoutCommand { get; }

    /// <summary>Событие: пользователь вышел из системы.</summary>
    public event Action? LogoutRequested;
}
