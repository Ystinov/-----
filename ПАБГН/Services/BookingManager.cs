using Microsoft.EntityFrameworkCore;
using ПАБГН.Data;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Управление жизненным циклом броней. Ядро бизнес-логики приложения.</summary>
public class BookingManager(HotelDbContext db, PricingService pricing)
{
    /// <summary>Статусы, при которых бронь блокирует номер.</summary>
    private static readonly BookingStatus[] Blocking =
        [BookingStatus.New, BookingStatus.Confirmed, BookingStatus.CheckedIn];

    // ───────────────────── Поиск и проверка доступности ─────────────────────

    /// <summary>
    /// Свободные номера на период [checkIn, checkOut) для указанного числа гостей.
    /// Номера на ремонте и занятые на пересекающиеся даты не возвращаются.
    /// </summary>
    public List<Room> SearchAvailableRooms(DateTime checkIn, DateTime checkOut, int guestsCount, int? categoryId = null)
    {
        ValidatePeriod(checkIn, checkOut);
        if (guestsCount < 1)
            throw new DomainException("Количество гостей должно быть не менее 1.");

        var busyRoomIds = db.Bookings
            .Where(b => Blocking.Contains(b.Status) && b.CheckIn < checkOut && checkIn < b.CheckOut)
            .Select(b => b.RoomId);

        return db.Rooms
            .Include(r => r.Category)
            .Where(r => r.Status != RoomStatus.Maintenance
                        && r.Category.Capacity >= guestsCount
                        && (categoryId == null || r.CategoryId == categoryId)
                        && !busyRoomIds.Contains(r.Id))
            .OrderBy(r => r.Floor).ThenBy(r => r.Number)
            .ToList();
    }

    /// <summary>
    /// Проверка пересечения периодов: периоды считаются полуинтервалами [начало, конец),
    /// поэтому заезд в день выезда предыдущего гостя не образует конфликта.
    /// </summary>
    private bool HasOverlappingBooking(int roomId, DateTime checkIn, DateTime checkOut, int? ignoreBookingId = null)
        => db.Bookings.Any(b => b.RoomId == roomId
                                && Blocking.Contains(b.Status)
                                && (ignoreBookingId == null || b.Id != ignoreBookingId)
                                && b.CheckIn < checkOut && checkIn < b.CheckOut);

    /// <summary>Проверяет, свободен ли номер на указанный период.</summary>
    public bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut)
    {
        ValidatePeriod(checkIn, checkOut);
        return !HasOverlappingBooking(roomId, checkIn, checkOut);
    }

    /// <summary>Стоимость проживания конкретного номера на период (без доп. услуг).</summary>
    public decimal CalculatePrice(int roomId, DateTime checkIn, DateTime checkOut)
    {
        var room = db.Rooms.Include(r => r.Category).FirstOrDefault(r => r.Id == roomId)
            ?? throw new DomainException("Номер не найден.");
        return pricing.CalculateAccommodation(room, checkIn, checkOut);
    }

    // ───────────────────── Создание и изменение брони ─────────────────────

    /// <summary>Создаёт бронь с проверкой всех бизнес-правил.</summary>
    public Booking Create(int roomId, int guestId, int userId, DateTime checkIn, DateTime checkOut,
                          int guestsCount = 1,
                          IEnumerable<(int ServiceId, int Quantity)>? services = null,
                          string? comment = null)
    {
        ValidatePeriod(checkIn, checkOut);

        var room = db.Rooms.Include(r => r.Category).FirstOrDefault(r => r.Id == roomId)
            ?? throw new DomainException("Выбранный номер не найден.");

        if (room.Status == RoomStatus.Maintenance)
            throw new DomainException($"Номер №{room.Number} находится на ремонте.");

        if (guestsCount > room.Category.Capacity)
            throw new DomainException(
                $"Категория «{room.Category.Name}» рассчитана максимум на {room.Category.Capacity} гост.");

        if (HasOverlappingBooking(roomId, checkIn, checkOut))
            throw new DomainException($"Номер №{room.Number} уже забронирован на пересекающиеся даты.");

        var booking = new Booking
        {
            Code = NextCode(),
            RoomId = roomId,
            GuestId = guestId,
            CreatedByUserId = userId,
            CheckIn = checkIn.Date,
            CheckOut = checkOut.Date,
            GuestsCount = guestsCount,
            Status = BookingStatus.New,
            Comment = comment
        };

        var chosen = new List<(Service Service, int Quantity)>();
        foreach (var (serviceId, quantity) in services ?? [])
        {
            var service = db.Services.Find(serviceId)
                ?? throw new DomainException($"Услуга id={serviceId} не найдена.");

            if (quantity < 1)
                throw new DomainException($"Количество для услуги «{service.Name}» должно быть больше нуля.");

            chosen.Add((service, quantity));
            booking.Services.Add(new BookingService
            {
                ServiceId = service.Id,
                Service = service,
                Quantity = quantity,
                PriceAtMoment = service.Price   // цена фиксируется на момент продажи
            });
        }

        booking.TotalPrice = pricing.CalculateTotal(room, checkIn, checkOut, chosen);

        db.Bookings.Add(booking);
        db.SaveChanges();
        return booking;
    }

    /// <summary>Подтверждает новую бронь.</summary>
    public void Confirm(int bookingId)
    {
        var b = Get(bookingId);
        if (b.Status != BookingStatus.New)
            throw new DomainException("Подтвердить можно только новую бронь.");

        b.Status = BookingStatus.Confirmed;
        db.SaveChanges();
    }

    /// <summary>Продлевает проживание с проверкой доступности номера на новый период.</summary>
    public void Extend(int bookingId, DateTime newCheckOut)
    {
        var b = Get(bookingId);
        if (newCheckOut.Date <= b.CheckOut)
            throw new DomainException("Новая дата выезда должна быть позже текущей.");

        if (HasOverlappingBooking(b.RoomId, b.CheckIn, newCheckOut.Date, ignoreBookingId: b.Id))
            throw new DomainException("Номер занят на запрашиваемый период — продление невозможно.");

        b.CheckOut = newCheckOut.Date;
        Recalculate(b);
        db.SaveChanges();
    }

    /// <summary>Отменяет бронь. После заселения отмена запрещена.</summary>
    public void Cancel(int bookingId, string? reason = null)
    {
        var b = Get(bookingId);
        if (b.Status is BookingStatus.CheckedIn or BookingStatus.CheckedOut)
            throw new DomainException("Нельзя отменить бронь после заселения гостя.");

        b.Status = BookingStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            b.Comment = string.IsNullOrWhiteSpace(b.Comment)
                ? $"Отмена: {reason}"
                : $"{b.Comment} | Отмена: {reason}";

        db.SaveChanges();
    }

    // ───────────────────── Заселение и выселение ─────────────────────

    /// <summary>Заселяет гостя: бронь → «Заселён», номер → «Занят».</summary>
    public void CheckIn(int bookingId)
    {
        var b = Get(bookingId);
        if (b.Status is not (BookingStatus.New or BookingStatus.Confirmed))
            throw new DomainException("Заселение возможно только для новой или подтверждённой брони.");

        if (b.Room.Status == RoomStatus.Maintenance)
            throw new DomainException($"Номер №{b.Room.Number} находится на ремонте.");

        b.Status = BookingStatus.CheckedIn;
        b.Room.Status = RoomStatus.Occupied;
        db.SaveChanges();
    }

    /// <summary>Выселяет гостя: бронь → «Выехал», номер → «Требует уборки».</summary>
    public void CheckOut(int bookingId)
    {
        var b = Get(bookingId);
        if (b.Status != BookingStatus.CheckedIn)
            throw new DomainException("Гость в этом номере не заселён.");

        b.Status = BookingStatus.CheckedOut;
        b.Room.Status = RoomStatus.Cleaning;
        db.SaveChanges();
    }

    // ───────────────────── Выборки ─────────────────────

    /// <summary>Все брони (при необходимости — только с указанным статусом).</summary>
    public List<Booking> GetAll(BookingStatus? status = null)
    {
        var query = db.Bookings
            .Include(b => b.Room).ThenInclude(r => r.Category)
            .Include(b => b.Guest)
            .AsQueryable();

        if (status is not null)
            query = query.Where(b => b.Status == status);

        return query.OrderByDescending(b => b.CheckIn).ToList();
    }

    /// <summary>Гости, проживающие в гостинице в текущий момент.</summary>
    public List<Booking> GetCurrentStay()
        => GetAll().Where(b => b.Status == BookingStatus.CheckedIn).ToList();

    /// <summary>Получает бронь по идентификатору со всеми связанными данными.</summary>
    public Booking Get(int id) => db.Bookings
        .Include(b => b.Room).ThenInclude(r => r.Category)
        .Include(b => b.Guest)
        .Include(b => b.Services).ThenInclude(s => s.Service)
        .FirstOrDefault(b => b.Id == id)
        ?? throw new DomainException($"Бронь id={id} не найдена.");

    /// <summary>Пересчитывает стоимость брони (после продления или изменения услуг).</summary>
    public void Recalculate(Booking booking)
    {
        var room = db.Rooms.Include(r => r.Category).First(r => r.Id == booking.RoomId);
        var items = booking.Services.Select(s => (s.Service, s.Quantity)).ToList();
        booking.TotalPrice = pricing.CalculateTotal(room, booking.CheckIn, booking.CheckOut, items);
    }

    // ───────────────────── Вспомогательные методы ─────────────────────

    /// <summary>Формирует следующий код брони в формате Б-{год}-{номер}.</summary>
    private string NextCode()
    {
        int year = DateTime.Now.Year;
        int count = db.Bookings.Count(b => b.CreatedAt.Year == year) + 1;
        return $"Б-{year}-{count:0000}";
    }

    /// <summary>Проверка корректности периода проживания.</summary>
    private static void ValidatePeriod(DateTime checkIn, DateTime checkOut)
    {
        if (checkOut.Date <= checkIn.Date)
            throw new DomainException("Дата выезда должна быть позже даты заезда.");

        if (checkIn.Date < DateTime.Today)
            throw new DomainException("Дата заезда не может быть в прошлом.");
    }
}
