using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Раздел «Брони»: список броней и действия по жизненному циклу.</summary>
public class BookingsViewModel : ViewModelBase, IRefreshable
{
    private readonly BookingManager _bookings;
    private readonly DialogService _dialogs;

    private readonly List<Booking> _all = [];

    private Booking? _selectedBooking;
    private FilterOption _selectedFilter = null!;
    private DateTime? _extendDate;

    public BookingsViewModel(BookingManager bookings, DialogService dialogs)
    {
        _bookings = bookings;
        _dialogs = dialogs;

        NewBookingCommand = new RelayCommand(_ => NavigateToNewBooking?.Invoke());
        ConfirmCommand = new RelayCommand(_ => Confirm(),
            _ => SelectedBooking?.Status == BookingStatus.New);
        CancelCommand = new RelayCommand(_ => Cancel(),
            _ => SelectedBooking?.Status is BookingStatus.New or BookingStatus.Confirmed);
        ExtendCommand = new RelayCommand(_ => Extend(),
            _ => SelectedBooking is not null
                 && SelectedBooking.Status is not (BookingStatus.Cancelled or BookingStatus.CheckedOut));
        CheckInCommand = new RelayCommand(_ => CheckIn(),
            _ => SelectedBooking?.Status is BookingStatus.New or BookingStatus.Confirmed);
        CheckOutCommand = new RelayCommand(_ => CheckOut(),
            _ => SelectedBooking?.Status == BookingStatus.CheckedIn);
        PayCommand = new RelayCommand(_ => OpenPayments(), _ => SelectedBooking is not null);

        _selectedFilter = StatusFilters[0];

        Refresh();
    }

    public ObservableCollection<Booking> Bookings { get; } = [];

    /// <summary>Вариант фильтра списка броней по статусу.</summary>
    public sealed record FilterOption(BookingStatus? Status, string Title);

    /// <summary>Варианты фильтра по статусу. Первый вариант — «Все».</summary>
    public IReadOnlyList<FilterOption> StatusFilters { get; } =
    [
        new(null, "Все"),
        new(BookingStatus.New, "Новая"),
        new(BookingStatus.Confirmed, "Подтверждена"),
        new(BookingStatus.CheckedIn, "Заселён"),
        new(BookingStatus.CheckedOut, "Выехал"),
        new(BookingStatus.Cancelled, "Отменена")
    ];

    public ICommand NewBookingCommand { get; }
    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ExtendCommand { get; }
    public ICommand CheckInCommand { get; }
    public ICommand CheckOutCommand { get; }
    public ICommand PayCommand { get; }

    /// <summary>Запрос перехода в раздел «Оплата» с выбранной бронью.</summary>
    public Action<Booking>? OpenPaymentsRequested { get; set; }

    /// <summary>Запрос перехода в раздел «Новая бронь».</summary>
    public Action? NavigateToNewBooking { get; set; }

    public Booking? SelectedBooking
    {
        get => _selectedBooking;
        set
        {
            if (!SetProperty(ref _selectedBooking, value)) return;

            ExtendDate = value?.CheckOut.AddDays(1);
            OnPropertyChanged(nameof(DetailsText));
        }
    }

    public FilterOption SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
                ApplyFilter();
        }
    }

    /// <summary>Новая дата выезда при продлении проживания.</summary>
    public DateTime? ExtendDate
    {
        get => _extendDate;
        set => SetProperty(ref _extendDate, value);
    }

    public string Statistics =>
        $"Всего броней: {_all.Count} · в отеле: {_all.Count(b => b.Status == BookingStatus.CheckedIn)} · " +
        $"ожидают заезда: {_all.Count(b => b.Status is BookingStatus.New or BookingStatus.Confirmed)} · " +
        $"выручка по броням: {_all.Where(b => b.Status != BookingStatus.Cancelled).Sum(b => b.TotalPrice):N2} ₽";

    /// <summary>Подробная информация по выбранной брони.</summary>
    public string DetailsText => SelectedBooking is null
        ? "Выберите бронь в таблице, чтобы увидеть подробности."
        : $"{SelectedBooking.Code} · {SelectedBooking.Room.Category.Name}, номер №{SelectedBooking.Room.Number} · " +
          $"{SelectedBooking.CheckIn:dd.MM.yyyy} — {SelectedBooking.CheckOut:dd.MM.yyyy} ({SelectedBooking.Nights} ноч.) · " +
          $"{SelectedBooking.GuestsCount} гост. · услуги: {(SelectedBooking.Services.Count == 0 ? "нет" : SelectedBooking.Services.Count.ToString())} · " +
          $"стоимость: {SelectedBooking.TotalPrice:N2} ₽ · оплачено: {SelectedBooking.PaidAmount:N2} ₽ · " +
          $"остаток: {SelectedBooking.Debt:N2} ₽";

    public void Refresh()
    {
        _all.Clear();
        _all.AddRange(_bookings.GetAll());

        ApplyFilter();
        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(DetailsText));
    }

    private void ApplyFilter()
    {
        var selectedId = SelectedBooking?.Id;

        Bookings.Clear();
        foreach (var booking in _all.Where(b => SelectedFilter.Status is null || b.Status == SelectedFilter.Status))
            Bookings.Add(booking);

        if (selectedId is not null)
            SelectedBooking = Bookings.FirstOrDefault(b => b.Id == selectedId);
    }

    private void Confirm()
    {
        if (SelectedBooking is null) return;

        if (_dialogs.TryRun(() => _bookings.Confirm(SelectedBooking.Id)))
        {
            _dialogs.Info("Бронь подтверждена.");
            Refresh();
        }
    }

    private void Cancel()
    {
        if (SelectedBooking is null) return;
        if (!_dialogs.Confirm($"Отменить бронь {SelectedBooking.Code}?")) return;

        if (_dialogs.TryRun(() => _bookings.Cancel(SelectedBooking.Id, "отменено оператором")))
        {
            _dialogs.Info("Бронь отменена.");
            Refresh();
        }
    }

    private void Extend()
    {
        if (SelectedBooking is null || ExtendDate is null) return;

        if (_dialogs.TryRun(() => _bookings.Extend(SelectedBooking.Id, ExtendDate.Value)))
        {
            _dialogs.Info("Проживание продлено, стоимость пересчитана.");
            Refresh();
        }
    }

    private void CheckIn()
    {
        if (SelectedBooking is null) return;

        if (_dialogs.TryRun(() => _bookings.CheckIn(SelectedBooking.Id)))
        {
            _dialogs.Info($"Гость заселён в номер №{SelectedBooking.Room.Number}.");
            Refresh();
        }
    }

    private void CheckOut()
    {
        if (SelectedBooking is null) return;

        if (_dialogs.TryRun(() => _bookings.CheckOut(SelectedBooking.Id)))
        {
            _dialogs.Info("Гость выехал, номер переведён на уборку.");
            Refresh();
        }
    }

    private void OpenPayments()
    {
        if (SelectedBooking is not null)
            OpenPaymentsRequested?.Invoke(SelectedBooking);
    }
}
