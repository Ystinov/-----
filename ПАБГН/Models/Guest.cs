namespace ПАБГН.Models;

/// <summary>Гость гостиницы.</summary>
public class Guest
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    /// <summary>Серия и номер паспорта.</summary>
    public string Passport { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? Address { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Booking> Bookings { get; set; } = [];

    public override string ToString() => $"{FullName} ({Phone})";
}
