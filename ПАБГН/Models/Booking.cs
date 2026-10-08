namespace ПАБГН.Models;

/// <summary>Бронь: связывает гостя, номер и период проживания.</summary>
public class Booking
{
    public int Id { get; set; }

    /// <summary>Читаемый код брони, например "Б-2026-0001".</summary>
    public string Code { get; set; } = string.Empty;

    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public int GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedBy { get; set; } = null!;

    /// <summary>Дата заезда. Период проживания — полуинтервал [CheckIn, CheckOut).</summary>
    public DateTime CheckIn { get; set; }

    /// <summary>Дата выезда.</summary>
    public DateTime CheckOut { get; set; }

    public int GuestsCount { get; set; } = 1;

    public BookingStatus Status { get; set; } = BookingStatus.New;

    /// <summary>Итоговая стоимость брони.</summary>
    public decimal TotalPrice { get; set; }

    /// <summary>Оплаченная сумма.</summary>
    public decimal PaidAmount { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<BookingService> Services { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];

    // ── Вычисляемые свойства (в БД не хранятся) ──

    /// <summary>Количество ночей проживания.</summary>
    public int Nights => (CheckOut.Date - CheckIn.Date).Days;

    /// <summary>Остаток к оплате.</summary>
    public decimal Debt => TotalPrice - PaidAmount;

    /// <summary>Состояние оплаты.</summary>
    public PaymentStatus PaymentStatus => PaidAmount <= 0
        ? PaymentStatus.Unpaid
        : PaidAmount < TotalPrice ? PaymentStatus.Partial
        : PaymentStatus.Paid;
}
