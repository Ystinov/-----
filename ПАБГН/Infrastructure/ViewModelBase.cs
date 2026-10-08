using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ПАБГН.Infrastructure;

/// <summary>
/// Базовый класс модели представления (MVVM): реализует уведомление об изменении свойств.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Сообщает интерфейсу об изменении свойства.</summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>Присваивает значение и уведомляет интерфейс, если значение изменилось.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }
}

/// <summary>
/// Раздел, который обновляет свои данные при каждом переходе на него.
/// </summary>
public interface IRefreshable
{
    void Refresh();
}
