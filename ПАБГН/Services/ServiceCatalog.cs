using ПАБГН.Data;
using ПАБГН.Models;

namespace ПАБГН.Services;

/// <summary>Справочник дополнительных услуг гостиницы.</summary>
public class ServiceCatalog(HotelDbContext db)
{
    public List<Service> GetAll() => db.Services.OrderBy(s => s.Name).ToList();

    public List<Service> GetActive() => db.Services
        .Where(s => s.IsActive)
        .OrderBy(s => s.Name)
        .ToList();

    /// <summary>Добавляет новую услугу или сохраняет изменения существующей.</summary>
    public void Save(Service service)
    {
        if (string.IsNullOrWhiteSpace(service.Name))
            throw new DomainException("Укажите название услуги.");
        if (service.Price < 0)
            throw new DomainException("Цена услуги не может быть отрицательной.");

        if (service.Id == 0) db.Services.Add(service);
        else db.Services.Update(service);

        db.SaveChanges();
    }

    /// <summary>Удаление возможно только для услуг, не использованных ни в одной брони.</summary>
    public void Delete(int serviceId)
    {
        var service = db.Services.Find(serviceId)
            ?? throw new DomainException("Услуга не найдена.");

        if (db.BookingServices.Any(bs => bs.ServiceId == serviceId))
            throw new DomainException("Услуга использовалась в бронях — удаление невозможно, снимите признак «Действует».");

        db.Services.Remove(service);
        db.SaveChanges();
    }
}
