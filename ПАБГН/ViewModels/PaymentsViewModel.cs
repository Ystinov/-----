using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>
/// Раздел «Оплата»: платежи и возвраты по выбранной брони.
/// </summary>
public class PaymentsViewModel : ViewModelBase, IRefreshable
{
    private readonly PaymentService _payments;
    private readonly DialogService _dialogs;

    private Booking? _booking;
    private decimal _amount;
    private PaymentType _type = PaymentType.Prepayment;
    private PaymentMethod _method = PaymentMethod.Cash;
    private string? _comment;

    public PaymentsViewModel(PaymentService payments, DialogService dialogs)
    {
        _payments = payments;
        _dialogs = dialogs;

        AddCommand = new RelayCommand(_ => Add(), _ => _booking is not null && Amount > 0);

        Refresh();
    }

    public ObservableCollection<Payment> Payments { get; } = [];

    public IReadOnlyList<PaymentType> Types { get; } = Enum.GetValues<PaymentType>();

    public IReadOnlyList<PaymentMethod> Methods { get; } = Enum.GetValues<PaymentMethod>();

    public ICommand AddCommand { get; }

    /// <summary>Открывает раздел для конкретной брони.</summary>
    public void Open(Booking booking)
    {
        _booking = booking;
        Refresh();
    }

    public string BookingInfo => _booking is null
        ? "Бронь не выбрана. Перейдите в раздел «Брони» и нажмите кнопку «Оплата»."
        : $"{_booking.Code} · {_booking.Guest.FullName} · номер №{_booking.Room.Number} · " +
          $"{_booking.CheckIn:dd.MM.yyyy} — {_booking.CheckOut:dd.MM.yyyy}";

    public bool HasBooking => _booking is not null;

    public decimal TotalPrice => _booking?.TotalPrice ?? 0m;

    public decimal PaidAmount => _booking?.PaidAmount ?? 0m;

    public decimal Debt => _booking?.Debt ?? 0m;

    public decimal Amount
    {
        get => _amount;
        set => SetProperty(ref _amount, value);
    }

    public PaymentType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public PaymentMethod Method
    {
        get => _method;
        set => SetProperty(ref _method, value);
    }

    public string? Comment
    {
        get => _comment;
        set => SetProperty(ref _comment, value);
    }

    public void Refresh()
    {
        Payments.Clear();

        if (_booking is not null)
            foreach (var payment in _payments.GetByBooking(_booking.Id))
                Payments.Add(payment);

        OnPropertyChanged(nameof(BookingInfo));
        OnPropertyChanged(nameof(HasBooking));
        OnPropertyChanged(nameof(TotalPrice));
        OnPropertyChanged(nameof(PaidAmount));
        OnPropertyChanged(nameof(Debt));
    }

    private void Add()
    {
        if (_booking is null) return;

        bool ok = _dialogs.TryRun(() => _payments.Add(_booking.Id, Amount, Type, Method, Comment));

        if (!ok) return;

        _dialogs.Info("Платёж зарегистрирован.");
        Amount = 0;
        Comment = null;
        Refresh();
    }
}
