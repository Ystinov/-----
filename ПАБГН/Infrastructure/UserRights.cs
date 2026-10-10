using ПАБГН.Models;

namespace ПАБГН.Infrastructure;

/// <summary>
/// Матрица прав доступа по ролям сотрудников.
///
/// Управляющий гостиницей — полный доступ: справочники, тарифы, отчёты, сотрудники.
/// Администратор службы приёма и размещения (СПиР) — работа с бронями, гостями,
/// оплатой и отчётами, но без ведения справочников и учётных записей.
/// Горничная — только изменение состояния номеров.
/// </summary>
public static class UserRights
{
    /// <summary>Название роли для интерфейса.</summary>
    public static string Title(UserRole role) => role switch
    {
        UserRole.Manager => "Управляющий гостиницей",
        UserRole.Receptionist => "Администратор СПиР",
        UserRole.Housekeeper => "Горничная",
        _ => role.ToString()
    };

    /// <summary>Оформление и изменение броней, заселение и выселение гостей.</summary>
    public static bool CanWorkWithBookings(UserRole role)
        => role is UserRole.Manager or UserRole.Receptionist;

    /// <summary>Картотека гостей и приём оплаты.</summary>
    public static bool CanWorkWithGuests(UserRole role)
        => role is UserRole.Manager or UserRole.Receptionist;

    /// <summary>Просмотр отчётов о загрузке и выручке.</summary>
    public static bool CanSeeReports(UserRole role)
        => role is UserRole.Manager or UserRole.Receptionist;

    /// <summary>Ведение справочника дополнительных услуг.</summary>
    public static bool CanEditCatalogs(UserRole role)
        => role == UserRole.Manager;

    /// <summary>Изменение номерного фонда: добавление номеров, категории, удаление.</summary>
    public static bool CanEditRooms(UserRole role)
        => role == UserRole.Manager;

    /// <summary>Управление учётными записями сотрудников.</summary>
    public static bool CanManageUsers(UserRole role)
        => role == UserRole.Manager;
}
