using System.Windows;
using ПАБГН.Services;

namespace ПАБГН.Infrastructure;

/// <summary>
/// Единая точка показа диалоговых окон. Позволяет моделям представления
/// не обращаться напрямую к MessageBox.
/// </summary>
public class DialogService
{
    public void Info(string message, string title = "Информация")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public void Error(string message, string title = "Ошибка")
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    public bool Confirm(string message, string title = "Подтверждение")
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
           == MessageBoxResult.Yes;

    /// <summary>
    /// Выполняет действие, перехватывая ошибки бизнес-правил и показывая их пользователю.
    /// Возвращает true, если действие выполнено успешно.
    /// </summary>
    public bool TryRun(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (DomainException ex)
        {
            Error(ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Error($"Непредвиденная ошибка: {ex.Message}");
            return false;
        }
    }
}
