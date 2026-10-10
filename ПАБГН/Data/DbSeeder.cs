using ПАБГН.Infrastructure;
using ПАБГН.Models;

namespace ПАБГН.Data;

/// <summary>Первичное заполнение БД демонстрационными данными.</summary>
public static class DbSeeder
{
    /// <summary>
    /// Заполняет справочники, номера, услуги и пользователей.
    /// Повторный запуск ничего не делает, если данные уже есть.
    /// </summary>
    public static void Seed(HotelDbContext db)
    {
        if (db.Rooms.Any()) return;

        // 1. Категории номеров
        var standard1 = new RoomCategory { Name = "Стандарт (1 место)", Capacity = 1, PricePerNight = 3500m };
        var standard2 = new RoomCategory { Name = "Стандарт (2 места)", Capacity = 2, PricePerNight = 5000m };
        var junior    = new RoomCategory { Name = "Полулюкс", Capacity = 2, PricePerNight = 6500m };
        var lux       = new RoomCategory { Name = "Люкс", Capacity = 3, PricePerNight = 12000m };
        db.Categories.AddRange(standard1, standard2, junior, lux);
        db.SaveChanges();

        // 2. Номера: 1-й этаж — стандарты, 2-й — стандарты и полулюксы, 3-й — люксы
        var rooms = new List<Room>();
        for (int i = 1; i <= 5; i++)
            rooms.Add(new Room { Number = $"1{i:00}", Floor = 1, CategoryId = i <= 2 ? standard1.Id : standard2.Id });
        for (int i = 1; i <= 5; i++)
            rooms.Add(new Room { Number = $"2{i:00}", Floor = 2, CategoryId = i <= 3 ? standard2.Id : junior.Id });
        for (int i = 1; i <= 3; i++)
            rooms.Add(new Room { Number = $"3{i:00}", Floor = 3, CategoryId = lux.Id });
        rooms.Add(new Room
        {
            Number = "106",
            Floor = 1,
            CategoryId = standard2.Id,
            Status = RoomStatus.Maintenance,
            Notes = "Ремонт санузла"
        });
        db.Rooms.AddRange(rooms);

        // 3. Дополнительные услуги
        db.Services.AddRange(
            new Service { Name = "Завтрак", Price = 450m, Unit = "чел." },
            new Service { Name = "Парковка", Price = 300m, Unit = "сутки" },
            new Service { Name = "Трансфер из аэропорта", Price = 1500m, Unit = "шт." },
            new Service { Name = "Прачечная", Price = 500m, Unit = "шт." });

        // 4. Пользователи (пароли хранятся в виде хеша PBKDF2)
        db.Users.AddRange(
            new User
            {
                Login = "manager",
                PasswordHash = PasswordHasher.Hash("manager123"),
                FullName = "Соколова М.В.",
                Role = UserRole.Manager
            },
            new User
            {
                Login = "reception",
                PasswordHash = PasswordHasher.Hash("reception123"),
                FullName = "Петров П.П.",
                Role = UserRole.Receptionist
            },
            new User
            {
                Login = "housekeeper",
                PasswordHash = PasswordHasher.Hash("housekeeper123"),
                FullName = "Сидорова Л.И.",
                Role = UserRole.Housekeeper
            });

        db.SaveChanges();
    }
}
