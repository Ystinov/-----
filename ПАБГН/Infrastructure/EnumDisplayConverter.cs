using System.Globalization;
using System.Windows.Data;
using ПАБГН.Models;

namespace ПАБГН.Infrastructure;

/// <summary>
/// Преобразует значения перечислений в русские подписи для интерфейса.
/// Пустое значение (фильтр «все») отображается как «Все».
/// </summary>
public class EnumDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        null => "Все",

        RoomStatus.Available => "Свободен",
        RoomStatus.Occupied => "Занят",
        RoomStatus.Cleaning => "Требует уборки",
        RoomStatus.Maintenance => "На ремонте",

        BookingStatus.New => "Новая",
        BookingStatus.Confirmed => "Подтверждена",
        BookingStatus.CheckedIn => "Заселён",
        BookingStatus.CheckedOut => "Выехал",
        BookingStatus.Cancelled => "Отменена",

        PaymentType.Prepayment => "Предоплата",
        PaymentType.Full => "Полная оплата",
        PaymentType.Refund => "Возврат",

        PaymentMethod.Cash => "Наличные",
        PaymentMethod.Card => "Банковская карта",
        PaymentMethod.Transfer => "Безналичный перевод",

        PaymentStatus.Unpaid => "Не оплачено",
        PaymentStatus.Partial => "Частично оплачено",
        PaymentStatus.Paid => "Оплачено",

        UserRole.Administrator => "Администратор",
        UserRole.Manager => "Менеджер",
        UserRole.Housekeeper => "Горничная",

        _ => value.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
