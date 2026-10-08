namespace ПАБГН.Models;

/// <summary>Платёж по брони (предоплата, полная оплата, возврат).</summary>
public class Payment
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    /// <summary>Сумма платежа. У возврата значение отрицательное.</summary>
    public decimal Amount { get; set; }

    public PaymentType Type { get; set; } = PaymentType.Prepayment;

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public DateTime PaidAt { get; set; } = DateTime.Now;

    public string? Comment { get; set; }
}
