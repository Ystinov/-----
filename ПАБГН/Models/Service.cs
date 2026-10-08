namespace ПАБГН.Models;

/// <summary>Дополнительная услуга (завтрак, парковка, трансфер…).</summary>
public class Service
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Единица измерения: шт., сутки, чел.</summary>
    public string Unit { get; set; } = "шт.";

    public bool IsActive { get; set; } = true;

    public List<BookingService> Bookings { get; set; } = [];

    public override string ToString() => $"{Name} — {Price:N2} ₽";
}
