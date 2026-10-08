using System.Windows.Input;

namespace ПАБГН.Infrastructure;

/// <summary>Команда MVVM: связывает действие модели представления с элементом интерфейса.</summary>
public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null) : ICommand
{
    private readonly Action<object?> _execute = execute
        ?? throw new ArgumentNullException(nameof(execute));

    private readonly Predicate<object?>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged
    {
        // Подписка на общее событие WPF: доступность команд пересчитывается
        // автоматически при любом взаимодействии пользователя с интерфейсом.
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>Заставляет интерфейс заново проверить доступность команды.</summary>
    public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
}
