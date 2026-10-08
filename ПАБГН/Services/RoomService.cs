using Microsoft.EntityFrameworkCore;
using ПАБГН.Data;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Справочник номерного фонда и категорий номеров.</summary>
public class RoomService(HotelDbContext db)
{
    /// <summary>Все номера с категориями, упорядоченные по этажам.</summary>
    public List<Room> GetAll() => db.Rooms
        .Include(r => r.Category)
        .OrderBy(r => r.Floor).ThenBy(r => r.Number)
        .ToList();

    /// <summary>Категории номеров, отсортированные по цене.</summary>
    public List<RoomCategory> GetCategories() => db.Categories
        .OrderBy(c => c.PricePerNight)
        .ToList();

    /// <summary>Добавляет новый номер или сохраняет изменения существующего.</summary>
    public void Save(Room room)
    {
        if (string.IsNullOrWhiteSpace(room.Number))
            throw new DomainException("Укажите номер комнаты.");

        bool duplicate = db.Rooms.Any(r => r.Number == room.Number && r.Id != room.Id);
        if (duplicate)
            throw new DomainException($"Номер №{room.Number} уже существует в системе.");

        if (room.Id == 0) db.Rooms.Add(room);
        else db.Rooms.Update(room);

        db.SaveChanges();
    }

    /// <summary>Удаление номера возможно только при отсутствии истории броней.</summary>
    public void Delete(int roomId)
    {
        var room = db.Rooms.Find(roomId)
            ?? throw new DomainException("Номер не найден.");

        if (db.Bookings.Any(b => b.RoomId == roomId))
            throw new DomainException("Нельзя удалить номер: по нему есть брони. Измените статус на «Ремонт».");

        db.Rooms.Remove(room);
        db.SaveChanges();
    }

    /// <summary>Меняет статус номера (свободен, занят, уборка, ремонт).</summary>
    public void SetStatus(int roomId, RoomStatus status)
    {
        var room = db.Rooms.Find(roomId)
            ?? throw new DomainException("Номер не найден.");

        room.Status = status;
        db.SaveChanges();
    }
}
