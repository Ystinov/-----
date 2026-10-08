using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Раздел «Услуги»: справочник дополнительных услуг.</summary>
public class ServicesViewModel : ViewModelBase, IRefreshable
{
    private readonly ServiceCatalog _catalog;
    private readonly DialogService _dialogs;

    private Service? _selectedService;
    private string _formTitle = "Новая услуга";
    private int _editId;
    private string _editName = string.Empty;
    private decimal _editPrice;
    private string _editUnit = "шт.";
    private bool _editIsActive = true;

    public ServicesViewModel(ServiceCatalog catalog, DialogService dialogs)
    {
        _catalog = catalog;
        _dialogs = dialogs;

        NewCommand = new RelayCommand(_ => StartNew());
        SaveCommand = new RelayCommand(_ => Save());
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedService is not null);

        Refresh();
    }

    public ObservableCollection<Service> Services { get; } = [];

    public IReadOnlyList<string> Units { get; } = ["шт.", "чел.", "сутки"];

    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public Service? SelectedService
    {
        get => _selectedService;
        set
        {
            if (SetProperty(ref _selectedService, value))
                LoadForm(value);
        }
    }

    public string FormTitle
    {
        get => _formTitle;
        private set => SetProperty(ref _formTitle, value);
    }

    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }

    public decimal EditPrice
    {
        get => _editPrice;
        set => SetProperty(ref _editPrice, value);
    }

    public string EditUnit
    {
        get => _editUnit;
        set => SetProperty(ref _editUnit, value);
    }

    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }

    public void Refresh()
    {
        var selectedId = SelectedService?.Id;

        Services.Clear();
        foreach (var service in _catalog.GetAll())
            Services.Add(service);

        if (selectedId is not null)
            SelectedService = Services.FirstOrDefault(s => s.Id == selectedId);
    }

    private void StartNew()
    {
        _editId = 0;
        FormTitle = "Новая услуга";
        EditName = string.Empty;
        EditPrice = 0;
        EditUnit = "шт.";
        EditIsActive = true;
    }

    private void LoadForm(Service? service)
    {
        if (service is null) return;

        _editId = service.Id;
        FormTitle = $"Услуга: {service.Name}";
        EditName = service.Name;
        EditPrice = service.Price;
        EditUnit = service.Unit;
        EditIsActive = service.IsActive;
    }

    private void Save()
    {
        bool ok = _dialogs.TryRun(() =>
        {
            var service = _editId == 0
                ? new Service()
                : Services.First(s => s.Id == _editId);

            service.Name = EditName;
            service.Price = EditPrice;
            service.Unit = EditUnit;
            service.IsActive = EditIsActive;

            _catalog.Save(service);
        });

        if (!ok) return;

        _dialogs.Info("Услуга сохранена.");
        Refresh();
        StartNew();
    }

    private void Delete()
    {
        if (SelectedService is null) return;
        if (!_dialogs.Confirm($"Удалить услугу «{SelectedService.Name}»?")) return;

        if (_dialogs.TryRun(() => _catalog.Delete(SelectedService.Id)))
        {
            Refresh();
            StartNew();
        }
    }
}
