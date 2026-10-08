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

    /// <summary>Проверка прав: действие доступно только администратору.</summary>
    public void RequireAdministrator()
    {
        if (CurrentUser is null || !CurrentUser.IsAdministrator)
            throw new DomainException("Недостаточно прав: требуется роль администратора.");
    }
}
