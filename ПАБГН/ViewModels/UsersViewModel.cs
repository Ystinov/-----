using System.Collections.ObjectModel;
using System.Windows.Input;
using ПАБГН.Infrastructure;
using ПАБГН.Models;
using ПАБГН.Services;

namespace ПАБГН.ViewModels;

/// <summary>Раздел «Сотрудники»: учётные записи (доступен только администратору).</summary>
public class UsersViewModel : ViewModelBase, IRefreshable
{
    private readonly UserService _users;
    private readonly AuthService _auth;
    private readonly DialogService _dialogs;

    private User? _selectedUser;
    private string _formTitle = "Новый сотрудник";
    private int _editId;
    private string _editLogin = string.Empty;
    private string _editFullName = string.Empty;
    private UserRole _editRole = UserRole.Manager;
    private bool _editIsActive = true;
    private string _editPassword = string.Empty;

    public UsersViewModel(UserService users, AuthService auth, DialogService dialogs)
    {
        _users = users;
        _auth = auth;
        _dialogs = dialogs;

        NewCommand = new RelayCommand(_ => StartNew());
        SaveCommand = new RelayCommand(_ => Save());
        DeleteCommand = new RelayCommand(_ => Delete(), _ => SelectedUser is not null);

        Refresh();
    }

    public ObservableCollection<User> Users { get; } = [];

    public IReadOnlyList<UserRole> Roles { get; } = Enum.GetValues<UserRole>();

    public ICommand NewCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
                LoadForm(value);
        }
    }

    public string FormTitle
    {
        get => _formTitle;
        private set => SetProperty(ref _formTitle, value);
    }

    public string EditLogin
    {
        get => _editLogin;
        set => SetProperty(ref _editLogin, value);
    }

    public string EditFullName
    {
        get => _editFullName;
        set => SetProperty(ref _editFullName, value);
    }

    public UserRole EditRole
    {
        get => _editRole;
        set => SetProperty(ref _editRole, value);
    }

    public bool EditIsActive
    {
        get => _editIsActive;
        set => SetProperty(ref _editIsActive, value);
    }

    /// <summary>Пароль. Заполняется только при создании сотрудника или смене пароля.</summary>
    public string EditPassword
    {
        get => _editPassword;
        set => SetProperty(ref _editPassword, value);
    }

    public void Refresh()
    {
        if (_auth.CurrentUser?.IsManager != true)
            return;

        var selectedId = SelectedUser?.Id;

        Users.Clear();
        foreach (var user in _users.GetAll())
            Users.Add(user);

        if (selectedId is not null)
            SelectedUser = Users.FirstOrDefault(u => u.Id == selectedId);
    }

    private void StartNew()
    {
        _editId = 0;
        FormTitle = "Новый сотрудник";
        EditLogin = string.Empty;
        EditFullName = string.Empty;
        EditRole = UserRole.Manager;
        EditIsActive = true;
        EditPassword = string.Empty;
    }

    private void LoadForm(User? user)
    {
        if (user is null) return;

        _editId = user.Id;
        FormTitle = $"Сотрудник: {user.FullName}";
        EditLogin = user.Login;
        EditFullName = user.FullName;
        EditRole = user.Role;
        EditIsActive = user.IsActive;
        EditPassword = string.Empty;
    }

    private void Save()
    {
        string? password = string.IsNullOrWhiteSpace(EditPassword) ? null : EditPassword;

        bool ok = _dialogs.TryRun(() =>
        {
            _auth.RequireManager();
            _users.Save(_editId, EditLogin, EditFullName, EditRole, EditIsActive, password);
        });

        if (!ok) return;

        _dialogs.Info("Данные сотрудника сохранены.");
        Refresh();
        StartNew();
    }

    private void Delete()
    {
        if (SelectedUser is null) return;
        if (!_dialogs.Confirm($"Удалить учётную запись «{SelectedUser.Login}»?")) return;

        if (_dialogs.TryRun(() =>
            {
                _auth.RequireManager();
                _users.Delete(SelectedUser.Id);
            }))
        {
            Refresh();
            StartNew();
        }
    }
}
