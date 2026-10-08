using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Раздел «Номера»: ведение номерного фонда.</summary>
public class RoomsViewModel : ViewModelBase, IRefreshable
{
    private readonly RoomService _rooms;
    private readonly DialogService _dialogs;

    private readonly List<Room> _all = [];

    private Room? _selectedRoom;
    private string _search = string.Empty;

    private string _formTitle = "Новый номер";
    private int _editId;
    private string _editNumber = string.Empty;
    private int _editFloor = 1;
    private RoomCategory? _editCategory;
    private RoomStatus _editStatus = RoomStatus.Available;
    private string? _editNotes;

    public RoomsViewModel(RoomService rooms, DialogService dialogs)
    {
        _rooms = rooms;
        _dialogs = dialogs;

        Categories = _rooms.GetCategories();
        _editCategory = Categories.FirstOrDefault();

        NewCommand = new RelayCommand(_ => StartNew());
        SaveCommand = new RelayCommand(_ => Save());
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedRoom is not null);
        SetStatusCommand = new RelayCommand(OnSetStatus, p => SelectedRoom is not null && p is string);

        Refresh();
    }

    public ObservableCollection<Room> Rooms { get; } = [];

    public List<RoomCategory> Categories { get; }

    public IReadOnlyList<RoomStatus> Statuses { get; } = Enum.GetValues<RoomStatus>();

    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SetStatusCommand { get; }

    public Room? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (SetProperty(ref _selectedRoom, value))
                LoadForm(value);
        }
    }

    /// <summary>Фильтр списка по номеру, категории или этажу.</summary>
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

    public string EditNumber
    {
        get => _editNumber;
        set => SetProperty(ref _editNumber, value);
    }

    public int EditFloor
    {
        get => _editFloor;
        set => SetProperty(ref _editFloor, value);
    }

    public RoomCategory? EditCategory
    {
        get => _editCategory;
        set
        {
            if (SetProperty(ref _editCategory, value))
                OnPropertyChanged(nameof(EditPrice));
        }
    }

    public RoomStatus EditStatus
    {
        get => _editStatus;
        set => SetProperty(ref _editStatus, value);
    }

    public string? EditNotes
    {
        get => _editNotes;
        set => SetProperty(ref _editNotes, value);
    }

    /// <summary>Цена за ночь выбранной категории.</summary>
    public string EditPrice => EditCategory is null ? "—" : $"{EditCategory.PricePerNight:N2} ₽ / ночь";

    /// <summary>Краткая сводка по номерному фонду.</summary>
    public string Summary =>
        $"Всего номеров: {_all.Count} · свободно: {_all.Count(r => r.Status == RoomStatus.Available)} · " +
        $"занято: {_all.Count(r => r.Status == RoomStatus.Occupied)} · " +
        $"требуют уборки: {_all.Count(r => r.Status == RoomStatus.Cleaning)} · " +
        $"на ремонте: {_all.Count(r => r.Status == RoomStatus.Maintenance)}";

    public void Refresh()
    {
        _all.Clear();
        _all.AddRange(_rooms.GetAll());
        ApplyFilter();
        OnPropertyChanged(nameof(Summary));

        if (SelectedRoom is not null && _all.All(r => r.Id != SelectedRoom.Id))
            SelectedRoom = null;
    }

    private void ApplyFilter()
    {
        var term = _search.Trim().ToLower();

        var filtered = string.IsNullOrEmpty(term)
            ? _all
            : _all.Where(r => r.Number.ToLower().Contains(term)
                              || r.Category.Name.ToLower().Contains(term)
                              || r.Floor.ToString().Contains(term)).ToList();

        Rooms.Clear();
        foreach (var room in filtered)
            Rooms.Add(room);
    }

    private void StartNew()
    {
        _editId = 0;
        FormTitle = "Новый номер";
        EditNumber = string.Empty;
        EditFloor = 1;
        EditCategory = Categories.FirstOrDefault();
        EditStatus = RoomStatus.Available;
        EditNotes = null;
    }

    private void LoadForm(Room? room)
    {
        if (room is null) return;

        _editId = room.Id;
        FormTitle = $"Номер №{room.Number}";
        EditNumber = room.Number;
        EditFloor = room.Floor;
        EditCategory = Categories.FirstOrDefault(c => c.Id == room.CategoryId);
        EditStatus = room.Status;
        EditNotes = room.Notes;
    }

    private void Save()
    {
        bool ok = _dialogs.TryRun(() =>
        {
            if (EditCategory is null)
                throw new DomainException("Выберите категорию номера.");

            var room = _editId == 0 ? new Room() : _all.First(r => r.Id == _editId);
            room.Number = EditNumber;
            room.Floor = EditFloor;
            room.CategoryId = EditCategory.Id;
            room.Status = EditStatus;
            room.Notes = EditNotes;

            _rooms.Save(room);
        });

        if (!ok) return;

        _dialogs.Info("Данные номера сохранены.");
        Refresh();
        StartNew();
    }

    private void Delete()
    {
        if (SelectedRoom is null) return;
        if (!_dialogs.Confirm($"Удалить номер №{SelectedRoom.Number}?")) return;

        if (_dialogs.TryRun(() => _rooms.Delete(SelectedRoom.Id)))
        {
            Refresh();
            StartNew();
        }
    }

    private void OnSetStatus(object? parameter)
    {
        if (SelectedRoom is null || parameter is not string value) return;
        if (!Enum.TryParse<RoomStatus>(value, out var status)) return;

        _dialogs.TryRun(() => _rooms.SetStatus(SelectedRoom.Id, status));
        Refresh();
    }
}
