using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>
/// Раздел «Новая бронь»: подбор свободного номера на период, выбор гостя
/// и дополнительных услуг, расчёт стоимости и оформление брони.
/// </summary>
public class NewBookingViewModel : ViewModelBase, IRefreshable
{
    /// <summary>Категория-заглушка для варианта «Любая категория».</summary>
    private static readonly RoomCategory AnyCategory = new() { Id = 0, Name = "Любая категория" };

    private readonly BookingManager _bookingManager;
    private readonly RoomService _roomService;
    private readonly GuestService _guestService;
    private readonly ServiceCatalog _catalog;
    private readonly PricingService _pricing;
    private readonly AuthService _auth;
    private readonly DialogService _dialogs;

    private DateTime? _checkIn = DateTime.Today;
    private DateTime? _checkOut = DateTime.Today.AddDays(1);
    private int _guestsCount = 1;
    private RoomCategory? _categoryFilter = AnyCategory;
    private Room? _selectedRoom;
    private string _guestSearch = string.Empty;
    private Guest? _selectedGuest;
    private string? _comment;

    private decimal _accommodationPrice;
    private decimal _servicesPrice;
    private decimal _totalPrice;

    public NewBookingViewModel(BookingManager bookingManager,
                               RoomService roomService,
                               GuestService guestService,
                               ServiceCatalog catalog,
                               PricingService pricing,
                               AuthService auth,
                               DialogService dialogs)
    {
        _bookingManager = bookingManager;
        _roomService = roomService;
        _guestService = guestService;
        _catalog = catalog;
        _pricing = pricing;
        _auth = auth;
        _dialogs = dialogs;

        SearchCommand = new RelayCommand(_ => Search());
        CreateCommand = new RelayCommand(_ => Create(),
            _ => SelectedRoom is not null && SelectedGuest is not null);
        ResetCommand = new RelayCommand(_ => Refresh());

        Refresh();
    }

    // ── Данные для выбора ──

    public ObservableCollection<RoomCategory> CategoryFilters { get; } = [];

    public ObservableCollection<Room> AvailableRooms { get; } = [];

    public ObservableCollection<Guest> Guests { get; } = [];

    public ObservableCollection<ServiceSelection> Services { get; } = [];

    // ── Команды ──

    public ICommand SearchCommand { get; }
    public ICommand CreateCommand { get; }
    public ICommand ResetCommand { get; }

    /// <summary>Событие: бронь успешно оформлена.</summary>
    public event Action<Booking>? BookingCreated;

    // ── Параметры брони ──

    public DateTime? CheckIn
    {
        get => _checkIn;
        set
        {
            if (!SetProperty(ref _checkIn, value)) return;

            if (value is not null && (_checkOut is null || _checkOut <= value))
                CheckOut = value.Value.AddDays(1);

            ResetSearch();
            Recalculate();
        }
    }

    public DateTime? CheckOut
    {
        get => _checkOut;
        set
        {
            if (!SetProperty(ref _checkOut, value)) return;
            ResetSearch();
            Recalculate();
        }
    }

    public int GuestsCount
    {
        get => _guestsCount;
        set
        {
            if (!SetProperty(ref _guestsCount, value)) return;
            ResetSearch();
            Recalculate();
        }
    }

    public RoomCategory? CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (!SetProperty(ref _categoryFilter, value)) return;
            ResetSearch();
            Recalculate();
        }
    }

    public Room? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (!SetProperty(ref _selectedRoom, value)) return;
            Recalculate();
        }
    }

    public string GuestSearch
    {
        get => _guestSearch;
        set
        {
            if (SetProperty(ref _guestSearch, value))
                FilterGuests();
        }
    }

    public Guest? SelectedGuest
    {
        get => _selectedGuest;
        set => SetProperty(ref _selectedGuest, value);
    }

    public string? Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    // ── Расчёт стоимости ──

    public int Nights => _checkIn is null || _checkOut is null || _checkOut <= _checkIn
        ? 0
        : (_checkOut.Value.Date - _checkIn.Value.Date).Days;

    public decimal AccommodationPrice
    {
        get => _accommodationPrice;
        private set => SetProperty(ref _accommodationPrice, value);
    }

    public decimal ServicesPrice
    {
        get => _servicesPrice;
        private set => SetProperty(ref _servicesPrice, value);
    }

    public decimal TotalPrice
    {
        get => _totalPrice;
        private set => SetProperty(ref _totalPrice, value);
    }

    /// <summary>Пояснение к расчёту стоимости.</summary>
    public string PriceDetails
    {
        get
        {
            if (SelectedRoom is null || Nights <= 0 || _checkIn is null)
                return "Выберите номер и период — стоимость рассчитается автоматически.";

            var text = $"{Nights} ноч. × {SelectedRoom.Category.PricePerNight:N2} ₽ · " +
                       $"сезонный коэффициент ×{_pricing.SeasonCoefficient(_checkIn.Value):0.00}";

            var discount = _pricing.LongStayDiscount(Nights);
            if (discount > 0)
                text += $" · скидка за длительное проживание {discount:P0}";
            if (ServicesPrice > 0)
                text += $" · дополнительные услуги {ServicesPrice:N2} ₽";

            return text;
        }
    }

    // ── Обновление данных раздела ──

    public void Refresh()
    {
        int selectedCategoryId = CategoryFilter?.Id ?? 0;
        CategoryFilters.Clear();
        CategoryFilters.Add(AnyCategory);
        foreach (var category in _roomService.GetCategories())
            CategoryFilters.Add(category);

        _categoryFilter = CategoryFilters.FirstOrDefault(c => c.Id == selectedCategoryId) ?? AnyCategory;
        OnPropertyChanged(nameof(CategoryFilter));

        FilterGuests();

        Services.Clear();
        foreach (var service in _catalog.GetActive())
            Services.Add(new ServiceSelection(service, Recalculate));

        ResetSearch();
        Recalculate();
    }

    private void FilterGuests()
    {
        int? selectedId = SelectedGuest?.Id;

        Guests.Clear();
        foreach (var guest in _guestService.Search(_guestSearch))
            Guests.Add(guest);

        if (selectedId is not null)
            SelectedGuest = Guests.FirstOrDefault(g => g.Id == selectedId);
    }

    private void ResetSearch()
    {
        AvailableRooms.Clear();
        SelectedRoom = null;
    }

    private List<(Service Service, int Quantity)> SelectedServices()
        => Services.Where(s => s.IsSelected && s.Quantity > 0)
                   .Select(s => (s.Service, s.Quantity))
                   .ToList();

    private void Recalculate()
    {
        OnPropertyChanged(nameof(Nights));

        if (SelectedRoom is null || _checkIn is null || _checkOut is null || Nights <= 0)
        {
            AccommodationPrice = 0;
            ServicesPrice = 0;
            TotalPrice = 0;
        }
        else
        {
            AccommodationPrice = _pricing.CalculateAccommodation(SelectedRoom, _checkIn.Value, _checkOut.Value);
            ServicesPrice = _pricing.CalculateServices(SelectedServices());
            TotalPrice = AccommodationPrice + ServicesPrice;
        }

        OnPropertyChanged(nameof(PriceDetails));
    }

    // ── Действия ──

    private void Search()
    {
        if (_checkIn is null || _checkOut is null)
        {
            _dialogs.Error("Укажите даты заезда и выезда.");
            return;
        }

        int? categoryId = CategoryFilter is null || CategoryFilter.Id == 0 ? null : CategoryFilter.Id;

        _dialogs.TryRun(() =>
        {
            var rooms = _bookingManager.SearchAvailableRooms(_checkIn.Value, _checkOut.Value, GuestsCount, categoryId);

            ResetSearch();
            foreach (var room in rooms)
                AvailableRooms.Add(room);

            if (AvailableRooms.Count == 0)
                _dialogs.Info("На выбранный период свободных номеров нет. Измените даты или число гостей.");
            else
                SelectedRoom = AvailableRooms[0];
        });
    }

    private void Create()
    {
        if (_checkIn is null || _checkOut is null)
        {
            _dialogs.Error("Укажите период проживания.");
            return;
        }

        if (SelectedRoom is null)
        {
            _dialogs.Error("Выберите свободный номер из результатов подбора.");
            return;
        }

        if (SelectedGuest is null)
        {
            _dialogs.Error("Выберите гостя из списка.");
            return;
        }

        if (_auth.CurrentUser is null)
        {
            _dialogs.Error("Сессия истекла — выполните вход заново.");
            return;
        }

        Booking? created = null;

        bool ok = _dialogs.TryRun(() =>
        {
            var services = SelectedServices().Select(s => (ServiceId: s.Service.Id, Quantity: s.Quantity));
            created = _bookingManager.Create(SelectedRoom.Id, SelectedGuest.Id, _auth.CurrentUser.Id,
                                             _checkIn.Value, _checkOut.Value, GuestsCount, services, Comment);
        });

        if (!ok || created is null) return;

        _dialogs.Info($"Бронь {created.Code} оформлена.\nСтоимость проживания: {created.TotalPrice:N2} ₽");
        BookingCreated?.Invoke(created);
    }
}

/// <summary>Выбранная дополнительная услуга с количеством.</summary>
public class ServiceSelection : ViewModelBase
{
    private readonly Action _changed;

    private bool _isSelected;
    private int _quantity = 1;

    public ServiceSelection(Service service, Action changed)
    {
        Service = service;
        _changed = changed;
    }

    public Service Service { get; }

    public string Title => $"{Service.Name} — {Service.Price:N2} ₽ / {Service.Unit}";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
                _changed();
        }
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
                _changed();
        }
    }
}
