using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Модель представления окна входа в систему.</summary>
public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _auth;

    private string _login = string.Empty;
    private string _error = string.Empty;

    public LoginViewModel(AuthService auth) => _auth = auth;

    /// <summary>Логин сотрудника.</summary>
    public string Login
    {
        get => _login;
        set => SetProperty(ref _login, value);
    }

    /// <summary>Текст ошибки входа (пустой, если ошибки нет).</summary>
    public string Error
    {
        get => _error;
        private set => SetProperty(ref _error, value);
    }

    /// <summary>Событие возникает после успешного входа.</summary>
    public event Action<User>? LoginSucceeded;

    /// <summary>
    /// Проверяет логин и пароль. Пароль передаётся из PasswordBox,
    /// так как это свойство нельзя связать с моделью представления.
    /// </summary>
    public bool TryLogin(string password)
    {
        try
        {
            Error = string.Empty;
            var user = _auth.Login(Login, password);
            LoginSucceeded?.Invoke(user);
            return true;
        }
        catch (DomainException ex)
        {
            Error = ex.Message;
            return false;
        }
    }
}
