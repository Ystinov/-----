using Microsoft.EntityFrameworkCore;
using ПАБГН.Infrastructure;
using ПАБГН.Models;

namespace ПАБГН.Data;

/// <summary>Контекст доступа к данным гостиницы (СУБД SQLite).</summary>
public class HotelDbContext(DbContextOptions<HotelDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RoomCategory> Categories => Set<RoomCategory>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<BookingService> BookingServices => Set<BookingService>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);

        // Перечисления храним строками — содержимое БД читаемо при просмотре
        mb.Entity<Room>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Booking>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        mb.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Payment>().Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        mb.Entity<Payment>().Property(x => x.Method).HasConversion<string>().HasMaxLength(20);

        // Денежные суммы храним как REAL (double): SQLite не поддерживает decimal,
        // а хранение в TEXT заставляет EF Core сортировать суммы через decimal.Parse
        // с текущей локалью системы — при русской локали это даёт FormatException.
        // Такой способ рекомендован документацией EF Core для SQLite.
        mb.Entity<RoomCategory>().Property(x => x.PricePerNight).HasConversion<double>();
        mb.Entity<Service>().Property(x => x.Price).HasConversion<double>();
        mb.Entity<BookingService>().Property(x => x.PriceAtMoment).HasConversion<double>();
        mb.Entity<Booking>().Property(x => x.TotalPrice).HasConversion<double>();
        mb.Entity<Booking>().Property(x => x.PaidAmount).HasConversion<double>();
        mb.Entity<Payment>().Property(x => x.Amount).HasConversion<double>();

        // Уникальность и индексы, ускоряющие поиск
        mb.Entity<Room>().HasIndex(x => x.Number).IsUnique();
        mb.Entity<User>().HasIndex(x => x.Login).IsUnique();
        mb.Entity<Guest>().HasIndex(x => x.Passport);
        mb.Entity<Booking>().HasIndex(x => new { x.RoomId, x.CheckIn, x.CheckOut });

        // Запрет каскадного удаления значимых данных
        mb.Entity<Booking>().HasOne(b => b.Room).WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Booking>().HasOne(b => b.Guest).WithMany(g => g.Bookings)
            .HasForeignKey(b => b.GuestId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<Booking>().HasOne(b => b.CreatedBy).WithMany(u => u.CreatedBookings)
            .HasForeignKey(b => b.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        mb.Entity<BookingService>().HasOne(x => x.Booking).WithMany(b => b.Services)
            .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);
        mb.Entity<Payment>().HasOne(x => x.Booking).WithMany(b => b.Payments)
            .HasForeignKey(x => x.BookingId).OnDelete(DeleteBehavior.Cascade);

        // Вычисляемые свойства не являются колонками БД
        mb.Entity<Booking>().Ignore(b => b.Nights).Ignore(b => b.Debt).Ignore(b => b.PaymentStatus);
        mb.Entity<BookingService>().Ignore(x => x.Total);
        mb.Entity<User>().Ignore(u => u.IsManager);
    }
}
