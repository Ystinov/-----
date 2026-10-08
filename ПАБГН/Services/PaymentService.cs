using ПАБГН.Data;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Учёт оплат по брони.</summary>
public class PaymentService(HotelDbContext db, BookingManager bookings)
{
    /// <summary>Добавляет платёж или возврат и пересчитывает оплаченную сумму брони.</summary>
    public Payment Add(int bookingId, decimal amount, PaymentType type, PaymentMethod method, string? comment = null)
    {
        var booking = bookings.Get(bookingId);

        if (type == PaymentType.Refund)
        {
            decimal paid = PaidAmount(bookingId);
            if (amount > paid)
                throw new DomainException($"Возврат {amount:N2} ₽ превышает оплаченную сумму {paid:N2} ₽.");
        }
        else if (amount <= 0)
        {
            throw new DomainException("Сумма платежа должна быть больше нуля.");
        }

        var payment = new Payment
        {
            BookingId = bookingId,
            Amount = type == PaymentType.Refund ? -amount : amount,
            Type = type,
            Method = method,
            Comment = comment
        };

        db.Payments.Add(payment);
        booking.PaidAmount = PaidAmount(bookingId) + payment.Amount;
        db.SaveChanges();
        return payment;
    }

    /// <summary>
    /// Сумма всех платежей по брони (возвраты учитываются со знаком минус).
    /// Итог считается в памяти: суммы хранятся в SQLite как REAL,
    /// и SQL-агрегат потребовал бы обратного преобразования к decimal.
    /// </summary>
    public decimal PaidAmount(int bookingId)
        => db.Payments
            .Where(p => p.BookingId == bookingId)
            .Select(p => p.Amount)
            .AsEnumerable()
            .Sum();

    /// <summary>История платежей по брони.</summary>
    public List<Payment> GetByBooking(int bookingId)
        => db.Payments.Where(p => p.BookingId == bookingId).OrderBy(p => p.PaidAt).ToList();
}
