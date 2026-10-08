using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Расчёт стоимости проживания и дополнительных услуг.</summary>
public class PricingService
{
    /// <summary>Сезонный коэффициент (упрощённо — по месяцу заезда).</summary>
    public decimal SeasonCoefficient(DateTime checkIn) => checkIn.Month switch
    {
        6 or 7 or 8 => 1.30m,   // высокий сезон
        12 or 1     => 1.20m,   // праздничные дни
        _           => 1.00m
    };

    /// <summary>Скидка за длительное проживание.</summary>
    public decimal LongStayDiscount(int nights) => nights switch
    {
        >= 14 => 0.10m,
        >= 7  => 0.05m,
        _     => 0m
    };

    /// <summary>Количество ночей проживания.</summary>
    public int Nights(DateTime checkIn, DateTime checkOut) => (checkOut.Date - checkIn.Date).Days;

    /// <summary>Стоимость проживания с учётом сезонного коэффициента и скидки.</summary>
    public decimal CalculateAccommodation(Room room, DateTime checkIn, DateTime checkOut)
    {
        int nights = Nights(checkIn, checkOut);
        decimal sum = nights * room.Category.PricePerNight * SeasonCoefficient(checkIn);
        return Math.Round(sum * (1 - LongStayDiscount(nights)), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Стоимость дополнительных услуг.</summary>
    public decimal CalculateServices(IEnumerable<(Service Service, int Quantity)> items)
        => items.Sum(i => i.Service.Price * i.Quantity);

    /// <summary>Полная стоимость брони: проживание + услуги.</summary>
    public decimal CalculateTotal(Room room, DateTime checkIn, DateTime checkOut,
                                  IEnumerable<(Service Service, int Quantity)> items)
        => CalculateAccommodation(room, checkIn, checkOut) + CalculateServices(items);
}
