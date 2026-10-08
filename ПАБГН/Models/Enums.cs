namespace ПАБГН.Models;

/// <summary>Физическое состояние номера.</summary>
public enum RoomStatus
{
    Available = 0,    // Свободен
    Occupied = 1,     // Занят
    Cleaning = 2,     // Требует уборки
    Maintenance = 3   // На ремонте
}

/// <summary>Этап жизненного цикла брони.</summary>
public enum BookingStatus
{
    New = 0,          // Новая
    Confirmed = 1,    // Подтверждена
    CheckedIn = 2,    // Гость заселён
    CheckedOut = 3,   // Гость выехал
    Cancelled = 4     // Отменена
}

/// <summary>Тип платежа.</summary>
public enum PaymentType
{
    Prepayment = 0,   // Предоплата
    Full = 1,         // Полная оплата
    Refund = 2        // Возврат
}

/// <summary>Способ оплаты.</summary>
public enum PaymentMethod
{
    Cash = 0,         // Наличные
    Card = 1,         // Банковская карта
    Transfer = 2      // Безналичный перевод
}

/// <summary>Состояние оплаты брони.</summary>
public enum PaymentStatus
{
    Unpaid = 0,       // Не оплачено
    Partial = 1,      // Частично оплачено
    Paid = 2          // Оплачено
}

/// <summary>Роль пользователя системы.</summary>
public enum UserRole
{
    Administrator = 0,
    Manager = 1,
    Housekeeper = 2
}
