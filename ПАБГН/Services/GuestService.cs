using Microsoft.EntityFrameworkCore;
using ПАБГН.Data;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Работа с картотекой гостей.</summary>
public class GuestService(HotelDbContext db)
{
    /// <summary>Поиск гостей по ФИО, паспорту или телефону.</summary>
    public List<Guest> Search(string? term = null)
    {
        var query = db.Guests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            query = query.Where(g => g.FullName.ToLower().Contains(term)
                                     || g.Passport.Contains(term)
                                     || g.Phone.Contains(term));
        }

        return query.OrderBy(g => g.FullName).ToList();
    }

    /// <summary>Добавляет нового гостя или сохраняет изменения карточки.</summary>
    public void Save(Guest guest)
    {
        if (string.IsNullOrWhiteSpace(guest.FullName))
            throw new DomainException("Укажите ФИО гостя.");

        if (string.IsNullOrWhiteSpace(guest.Passport))
            throw new DomainException("Укажите паспортные данные.");

        if (guest.Id == 0) db.Guests.Add(guest);
        else db.Guests.Update(guest);

        db.SaveChanges();
    }

    /// <summary>История броней гостя.</summary>
    public List<Booking> GetHistory(int guestId) => db.Bookings
        .Include(b => b.Room).ThenInclude(r => r.Category)
        .Where(b => b.GuestId == guestId)
        .OrderByDescending(b => b.CheckIn)
        .ToList();

    /// <summary>Удаление возможно только для гостя, у которого нет броней.</summary>
    public void Delete(int guestId)
    {
        var guest = db.Guests.Find(guestId)
            ?? throw new DomainException("Гость не найден.");

        if (db.Bookings.Any(b => b.GuestId == guestId))
            throw new DomainException("У гостя есть брони — удаление невозможно.");

        db.Guests.Remove(guest);
        db.SaveChanges();
    }
}
