namespace ПАБГН.Models;

/// <summary>Гостиничный номер.</summary>
public class Room
{
    public int Id { get; set; }

    /// <summary>Номер комнаты: "101", "205", "312".</summary>
    public string Number { get; set; } = string.Empty;

    public int Floor { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.Available;

    public string? Notes { get; set; }

    public int CategoryId { get; set; }
    public RoomCategory Category { get; set; } = null!;

    public List<Booking> Bookings { get; set; } = [];

    public override string ToString() => $"№{Number} — {Category?.Name}";
}
