using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Win32;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Раздел «Отчёты»: загрузка номерного фонда, выручка и экспорт данных.</summary>
public class ReportsViewModel : ViewModelBase, IRefreshable
{
    private readonly ReportService _report;
    private readonly DialogService _dialogs;

    private DateTime? _from = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _to = DateTime.Today;
    private decimal _revenue;
    private double _occupancy;

    public ReportsViewModel(ReportService report, DialogService dialogs)
    {
        _report = report;
        _dialogs = dialogs;

        BuildCommand = new RelayCommand(_ => Build());
        ExportCommand = new RelayCommand(_ => Export(), _ => Bookings.Count > 0);

        Build();
    }

    public ObservableCollection<Booking> Bookings { get; } = [];

    public ICommand BuildCommand { get; }

    public ICommand ExportCommand { get; }

    public DateTime? From
    {
        get => _from;
        set => SetProperty(ref _from, value);
    }

    public DateTime? To
    {
        get => _to;
        set => SetProperty(ref _to, value);
    }

    /// <summary>Выручка за период (сумма платежей).</summary>
    public decimal Revenue
    {
        get => _revenue;
        private set => SetProperty(ref _revenue, value);
    }

    /// <summary>Средняя загрузка номерного фонда за период.</summary>
    public double Occupancy
    {
        get => _occupancy;
        private set
        {
            if (SetProperty(ref _occupancy, value))
                OnPropertyChanged(nameof(OccupancyText));
        }
    }

    public string OccupancyText => $"{Occupancy:P1}";

    public string Summary =>
        $"Период: {From:dd.MM.yyyy} — {To:dd.MM.yyyy} · броней в периоде: {Bookings.Count} · " +
        $"выручка: {Revenue:N2} ₽ · средняя загрузка: {OccupancyText}";

    public void Refresh() => Build();

    private void Build()
    {
        if (From is null || To is null)
        {
            _dialogs.Error("Укажите период отчёта.");
            return;
        }

        if (To < From)
        {
            _dialogs.Error("Дата окончания периода не может быть раньше даты начала.");
            return;
        }

        _dialogs.TryRun(() =>
        {
            Bookings.Clear();
            foreach (var booking in _report.Bookings(From.Value, To.Value))
                Bookings.Add(booking);

            Revenue = _report.Revenue(From.Value, To.Value);
            Occupancy = _report.Occupancy(From.Value, To.Value);
        });

        OnPropertyChanged(nameof(Summary));
    }

    /// <summary>
    /// Сохраняет отчёт в CSV. Диалог выбора файла — единственное обращение
    /// к Windows API из модели представления.
    /// </summary>
    private void Export()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранение отчёта",
            Filter = "Файл CSV (*.csv)|*.csv",
            FileName = $"Отчет_{DateTime.Today:yyyy-MM-dd}.csv"
        };

        if (dialog.ShowDialog() != true) return;

        if (_dialogs.TryRun(() => _report.ExportCsv(Bookings, dialog.FileName)))
            _dialogs.Info($"Отчёт сохранён:\n{dialog.FileName}");
    }
}
