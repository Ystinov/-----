using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ПАБГН.Data;

/// <summary>
/// Фабрика контекста, которая используется только инструментами EF Core
/// (команды <c>dotnet ef migrations add</c> и <c>dotnet ef database update</c>).
/// В самом приложении контекст создаётся через DI-контейнер (см. App.xaml.cs).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
{
    public HotelDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HotelDbContext>()
            .UseSqlite("Data Source=hotel.db")
            .Options;

        return new HotelDbContext(options);
    }
}
