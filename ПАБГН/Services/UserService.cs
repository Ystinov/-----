using ПАБГН.Data;
using ПАБГН.Infrastructure;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Управление учётными записями сотрудников (доступно администратору).</summary>
public class UserService(HotelDbContext db, AuthService auth)
{
    public List<User> GetAll() => db.Users.OrderBy(u => u.FullName).ToList();

    /// <summary>
    /// Добавляет нового пользователя (Id = 0) или сохраняет изменения существующего.
    /// Пароль передаётся только при установке/смене, иначе null.
    /// </summary>
    public void Save(int id, string login, string fullName, UserRole role, bool isActive, string? password)
    {
        login = login.Trim();

        if (string.IsNullOrWhiteSpace(login))
            throw new DomainException("Укажите логин.");
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("Укажите ФИО сотрудника.");
        if (db.Users.Any(u => u.Login == login && u.Id != id))
            throw new DomainException($"Логин «{login}» уже занят другим сотрудником.");

        User user;
        if (id == 0)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new DomainException("Для нового сотрудника задайте пароль.");
            user = new User();
            db.Users.Add(user);
        }
        else
        {
            user = db.Users.Find(id) ?? throw new DomainException("Пользователь не найден.");
        }

        user.Login = login;
        user.FullName = fullName;
        user.Role = role;
        user.IsActive = isActive;

        if (!string.IsNullOrWhiteSpace(password))
            user.PasswordHash = PasswordHasher.Hash(password);

        db.SaveChanges();
    }

    public void Delete(int id)
    {
        var user = db.Users.Find(id) ?? throw new DomainException("Пользователь не найден.");

        if (auth.CurrentUser?.Id == id)
            throw new DomainException("Нельзя удалить учётную запись, под которой выполнен вход.");

        if (user.Role == UserRole.Administrator
            && db.Users.Count(u => u.Role == UserRole.Administrator) <= 1)
            throw new DomainException("В системе должен остаться хотя бы один администратор.");

        db.Users.Remove(user);
        db.SaveChanges();
    }
}
