using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ПАБГН.Data;
using ПАБГН.Infrastructure;
using ПАБГН.Services;
using ПАБГН.ViewModels;
using ПАБГН.Views;

namespace ПАБГН;

/// <summary>
/// Точка входа приложения и композиционный корень (настройка DI-контейнера).
/// </summary>
public partial class App : Application
{
    /// <summary>Глобальный DI-контейнер приложения.</summary>
    public static ServiceProvider Services { get; private set; } = null!;

    public App()
    {
        // Любая необработанная ошибка в интерфейсе не должна «ронять» приложение молча
        DispatcherUnhandledException += OnUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Файл БД лежит рядом с исполняемым файлом — приложение не требует установки СУБД.
        var dbPath = Path.Combine(AppContext.BaseDirectory, "hotel.db");

        var collection = new ServiceCollection();
        collection.AddDbContext<HotelDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));

        // Службы предметной области
        collection.AddSingleton<AuthService>();
        collection.AddSingleton<DialogService>();
        collection.AddTransient<RoomService>();
        collection.AddTransient<GuestService>();
        collection.AddTransient<PricingService>();
        collection.AddTransient<BookingManager>();
        collection.AddTransient<PaymentService>();
        collection.AddTransient<ServiceCatalog>();
        collection.AddTransient<UserService>();
        collection.AddTransient<ReportService>();

        // Модели представления
        collection.AddTransient<LoginViewModel>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<RoomsViewModel>();
        collection.AddTransient<GuestsViewModel>();
        collection.AddTransient<BookingsViewModel>();
        collection.AddTransient<NewBookingViewModel>();
        collection.AddTransient<PaymentsViewModel>();
        collection.AddTransient<ServicesViewModel>();
        collection.AddTransient<UsersViewModel>();
        collection.AddTransient<ReportsViewModel>();

        // Окна
        collection.AddTransient<LoginWindow>();
        collection.AddTransient<MainWindow>();

        Services = collection.BuildServiceProvider();

        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();

            if (db.Database.GetMigrations().Any())
                db.Database.Migrate();      // применяем миграции
            else
                db.Database.EnsureCreated(); // если миграции ещё не созданы

            DbSeeder.Seed(db);              // демонстрационные данные при первом запуске
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось инициализировать базу данных:\n{ex.Message}",
                            "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Показываем окно входа: главное окно откроется после успешной аутентификации
        Services.GetRequiredService<LoginWindow>().Show();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
