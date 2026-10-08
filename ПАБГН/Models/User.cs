namespace ПАБГН.Models;

/// <summary>Пользователь системы — сотрудник гостиницы.</summary>
public class User
{
    public int Id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Manager;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Booking> CreatedBookings { get; set; } = [];

    /// <summary>Признак роли администратора.</summary>
    public bool IsAdministrator => Role == UserRole.Administrator;

    public override string ToString() => $"{FullName} ({Login})";
}
