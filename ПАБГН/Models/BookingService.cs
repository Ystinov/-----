namespace ПАБГН.Models;

/// <summary>
/// Позиция брони: выбранная дополнительная услуга с количеством
/// и ценой, зафиксированной на момент продажи.
/// </summary>
public class BookingService
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;

    public int Quantity { get; set; } = 1;

    /// <summary>Цена услуги на момент оформления брони.</summary>
    public decimal PriceAtMoment { get; set; }

    /// <summary>Стоимость позиции.</summary>
    public decimal Total => PriceAtMoment * Quantity;
}
