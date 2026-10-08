namespace ПАБГН.Models;

/// <summary>Категория номера (Стандарт, Полулюкс, Люкс и т. п.).</summary>
public class RoomCategory
{
    public int Id { get; set; }

    /// <summary>Название категории.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Максимальное число гостей.</summary>
    public int Capacity { get; set; }

    /// <summary>Базовая цена за одну ночь.</summary>
    public decimal PricePerNight { get; set; }

    public string? Description { get; set; }

    public List<Room> Rooms { get; set; } = [];

    public override string ToString() => Name;
}
