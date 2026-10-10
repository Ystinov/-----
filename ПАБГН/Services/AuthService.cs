using ПАБГН.Data;
using ПАБГН.Infrastructure;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Аутентификация сотрудников и хранение текущей сессии.</summary>
public class AuthService(HotelDbContext db)
{
    /// <summary>Пользователь, вошедший в систему (null, если вход не выполнен).</summary>
    public User? CurrentUser { get; private set; }

    /// <summary>Выполняет вход по логину и паролю.</summary>
    public User Login(string login, string password)
    {
        var user = db.Users.FirstOrDefault(u => u.Login == login);
        if (user is null || !user.IsActive || !PasswordHasher.Verify(password, user.PasswordHash))
            throw new DomainException("Неверный логин или пароль, либо учётная запись заблокирована.");

        CurrentUser = user;
        return user;
    }

    public void Logout() => CurrentUser = null;

    /// <summary>Проверка прав: действие доступно только управляющему гостиницей.</summary>
    public void RequireManager()
    {
        if (CurrentUser?.IsManager != true)
            throw new DomainException("Недостаточно прав: действие доступно только управляющему гостиницей.");
    }

    /// <summary>Проверка прав: брони оформляют управляющий и администратор СПиР.</summary>
    public void RequireBookingStaff()
    {
        if (CurrentUser is null || !UserRights.CanWorkWithBookings(CurrentUser.Role))
            throw new DomainException("Недостаточно прав для работы с бронями.");
    }
}
