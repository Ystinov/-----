namespace ПАБГН.Services;

/// <summary>
/// Нарушено бизнес-правило предметной области.
/// Сообщение показывается пользователю как есть (без стек-трейса).
/// </summary>
public class DomainException(string message) : Exception(message);
