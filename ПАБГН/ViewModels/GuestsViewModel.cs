using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Раздел «Гости»: картотека гостей и история их броней.</summary>
public class GuestsViewModel : ViewModelBase, IRefreshable
{
    private readonly GuestService _guests;
    private readonly DialogService _dialogs;

    private Guest? _selectedGuest;
    private string _search = string.Empty;

    private string _formTitle = "Новый гость";
    private int _editId;
    private string _editFullName = string.Empty;
    private string _editPassport = string.Empty;
    private string _editPhone = string.Empty;
    private string? _editEmail;
    private DateTime? _editBirthDate;
    private string? _editAddress;
    private string? _editNotes;

    public GuestsViewModel(GuestService guests, DialogService dialogs)
    {
        _guests = guests;
        _dialogs = dialogs;

        NewCommand = new RelayCommand(_ => StartNew());
        SaveCommand = new RelayCommand(_ => Save());
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedGuest is not null);

        Refresh();
        StartNew();
    }

    public ObservableCollection<Guest> Guests { get; } = [];

    /// <summary>История броней выбранного гостя.</summary>
    public ObservableCollection<Booking> History { get; } = [];

    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public Guest? SelectedGuest
    {
        get => _selectedGuest;
        set
        {
            if (!SetProperty(ref _selectedGuest, value)) return;

            LoadForm(value);
            LoadHistory(value);
        }
    }

    /// <summary>Поиск по ФИО, паспорту или телефону.</summary>
    public string Search
    {
        get => _search;
        set
        {
            if (SetProperty(ref _search, value))
                ApplyFilter();
        }
    }

    public string FormTitle
    {
        get => _formTitle;
        private set => SetProperty(ref _formTitle, value);
    }

    public string EditFullName
    {
        get => _editFullName;
        set => SetProperty(ref _editFullName, value);
    }

    public string EditPassport
    {
        get => _editPassport;
        set => SetProperty(ref _editPassport, value);
    }

    public string EditPhone
    {
        get => _editPhone;
        set => SetProperty(ref _editPhone, value);
    }

    public string? EditEmail
    {
        get => _editEmail;
        set => SetProperty(ref _editEmail, value);
    }

    public DateTime? EditBirthDate
    {
        get => _editBirthDate;
        set => SetProperty(ref _editBirthDate, value);
    }

    public string? EditAddress
    {
        get => _editAddress;
        set => SetProperty(ref _editAddress, value);
    }

    public string? EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    public string HistoryHeader => SelectedGuest is null
        ? "История броней"
        : $"История броней: {SelectedGuest.FullName}";

    public void Refresh()
    {
        ApplyFilter();

        if (SelectedGuest is not null)
            LoadHistory(SelectedGuest);
    }

    private void ApplyFilter()
    {
        var result = _guests.Search(_search);

        var selectedId = SelectedGuest?.Id;
        Guests.Clear();
        foreach (var guest in result)
            Guests.Add(guest);

        if (selectedId is not null)
            SelectedGuest = Guests.FirstOrDefault(g => g.Id == selectedId);
    }

    private void LoadHistory(Guest? guest)
    {
        History.Clear();
        if (guest is not null)
            foreach (var booking in _guests.GetHistory(guest.Id))
                History.Add(booking);

        OnPropertyChanged(nameof(HistoryHeader));
    }

    private void StartNew()
    {
        _editId = 0;
        FormTitle = "Новый гость";
        EditFullName = string.Empty;
        EditPassport = string.Empty;
        EditPhone = string.Empty;
        EditEmail = null;
        EditBirthDate = null;
        EditAddress = null;
        EditNotes = null;
    }

    private void LoadForm(Guest? guest)
    {
        if (guest is null) return;

        _editId = guest.Id;
        FormTitle = $"Карточка: {guest.FullName}";
        EditFullName = guest.FullName;
        EditPassport = guest.Passport;
        EditPhone = guest.Phone;
        EditEmail = guest.Email;
        EditBirthDate = guest.BirthDate;
        EditAddress = guest.Address;
        EditNotes = guest.Notes;
    }

    private void Save()
    {
        bool ok = _dialogs.TryRun(() =>
        {
            var guest = _editId == 0 ? new Guest() : _guests.Search(null).First(g => g.Id == _editId);

            guest.FullName = EditFullName;
            guest.Passport = EditPassport;
            guest.Phone = EditPhone;
            guest.Email = EditEmail;
            guest.BirthDate = EditBirthDate;
            guest.Address = EditAddress;
            guest.Notes = EditNotes;

            _guests.Save(guest);
        });

        if (!ok) return;

        _dialogs.Info("Карточка гостя сохранена.");
        _editId = 0;
        Refresh();
    }

    private void Delete()
    {
        if (SelectedGuest is null) return;
        if (!_dialogs.Confirm($"Удалить карточку гостя «{SelectedGuest.FullName}»?")) return;

        if (_dialogs.TryRun(() => _guests.Delete(SelectedGuest.Id)))
        {
            SelectedGuest = null;
            Refresh();
            StartNew();
        }
    }
}
