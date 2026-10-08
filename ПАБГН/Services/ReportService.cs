using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ПАБГН.Data;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Построение отчётов: загрузка номерного фонда, выручка, экспорт данных.</summary>
public class ReportService(HotelDbContext db)
{
    /// <summary>Сумма платежей за период (возвраты учитываются со знаком минус).</summary>
    public decimal Revenue(DateTime from, DateTime to)
        => db.Payments
            .Where(p => p.PaidAt >= from.Date && p.PaidAt < to.Date.AddDays(1))
            .Select(p => p.Amount)
            .AsEnumerable()
            .Sum();

    /// <summary>
    /// Средняя загрузка номерного фонда за период: доля занятых номеро-суток
    /// от общего числа номеро-суток. Возвращает значение от 0 до 1.
    /// </summary>
    public double Occupancy(DateTime from, DateTime to)
    {
        int totalRooms = db.Rooms.Count();
        if (totalRooms == 0) return 0;

        int days = (to.Date - from.Date).Days + 1;
        if (days <= 0) return 0;

        var bookings = db.Bookings
            .Where(b => b.Status != BookingStatus.Cancelled
                        && b.CheckIn < to.Date.AddDays(1)
                        && from.Date < b.CheckOut)
            .Select(b => new { b.CheckIn, b.CheckOut })
            .ToList();

        int occupied = 0;
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
            occupied += bookings.Count(b => b.CheckIn <= day && day < b.CheckOut);

        return (double)occupied / (totalRooms * days);
    }

    /// <summary>Брони, пересекающиеся с периодом.</summary>
    public List<Booking> Bookings(DateTime from, DateTime to) => db.Bookings
        .Include(b => b.Room).ThenInclude(r => r.Category)
        .Include(b => b.Guest)
        .Where(b => b.CheckIn <= to.Date && b.CheckOut >= from.Date)
        .OrderBy(b => b.CheckIn)
        .ToList();

    /// <summary>Формирует содержимое CSV-файла с разделителем «;» (открывается в Excel).</summary>
    public string BuildCsv(IEnumerable<Booking> bookings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Код;Номер;Категория;Гость;Заезд;Выезд;Ночей;Гостей;Статус;Стоимость;Оплачено;Остаток");

        foreach (var b in bookings)
        {
            sb.AppendLine(string.Join(';',
                b.Code,
                b.Room.Number,
                b.Room.Category.Name,
                b.Guest.FullName,
                b.CheckIn.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                b.CheckOut.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                b.Nights,
                b.GuestsCount,
                b.Status.ToString(),
                b.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture),
                b.PaidAmount.ToString("0.00", CultureInfo.InvariantCulture),
                b.Debt.ToString("0.00", CultureInfo.InvariantCulture)));
        }

        return sb.ToString();
    }

    /// <summary>Сохраняет отчёт в файл в кодировке UTF-8 с BOM (чтобы Excel видел кириллицу).</summary>
    public void ExportCsv(IEnumerable<Booking> bookings, string path)
        => File.WriteAllText(path, BuildCsv(bookings), new UTF8Encoding(true));
}
