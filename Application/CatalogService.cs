using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Entities;
using BLL.Models;
using System.Linq;

namespace BLL
{
    public class CatalogService
    {
        private readonly AppDbContext _context;

        public CatalogService(AppDbContext context)
        {
            _context = context;
        }


        // 1. Получение всех услуг
        public async Task<List<ServiceModel>> GetAllServicesAsync()
        {
            var entities = await _context.services
                .Include(s => s.resources)
                .Include(s => s.servicecategories)
                    .ThenInclude(sc => sc.category)
                .OrderBy(s => s.name)
                .ToListAsync();

            return entities.Select(ToServiceModel).ToList();
        }

        // 2. Получение услуги по ID с деталями
        public async Task<Services?> GetServiceDetailsAsync(int serviceId)
        {
            return await _context.services
                .Include(s => s.resources)
                .Include(s => s.servicecategories)
                    .ThenInclude(sc => sc.category)
                .FirstOrDefaultAsync(s => s.serviceid == serviceId);
        }

        // 3. Поиск услуг по названию
        public async Task<List<ServiceModel>> SearchServicesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllServicesAsync();

            var entities = await _context.services
                .Include(s => s.resources)
                .Include(s => s.servicecategories)
                    .ThenInclude(sc => sc.category)
                .Where(s => s.name.Contains(searchTerm))
                .OrderBy(s => s.name)
                .ToListAsync();

            return entities.Select(ToServiceModel).ToList();
        }

        // Вспомогательный метод для преобразования
        private ServiceModel ToServiceModel(Services entity)
        {
            if (entity == null) return null;

            return new ServiceModel
            {
                Id = entity.serviceid,
                Name = entity.name,
                Duration = entity.duration,
                WeekdayPrice = entity.weekdayprice,
                WeekendPrice = entity.weekendprice,
                StartTime = entity.starttime,
                EndTime = entity.endtime,
                Resources = entity.resources?.Select(r => new ResourceModel
                {
                    Id = r.resourceid,
                    Name = r.name,
                    Capacity = r.capacity
                }).ToList() ?? new List<ResourceModel>(),
                Categories = entity.servicecategories?.Select(sc => new CategoryModel
                {
                    Id = sc.category?.categoryid ?? 0,
                    Name = sc.category?.categoryname ?? "Без категории"
                }).ToList() ?? new List<CategoryModel>()
            };
        }

        // 4. Фильтрация по категории
        public async Task<List<Services>> GetServicesByCategoryAsync(int categoryId)
        {
            return await _context.services
                .Include(s => s.resources)
                .Include(s => s.servicecategories)
                    .ThenInclude(sc => sc.category)
                .Where(s => s.servicecategories.Any(sc => sc.categoryid == categoryId))
                .ToListAsync();
        }

        // 5. Получение всех категорий услуг
        public async Task<List<Categories>> GetAllCategoriesAsync()
        {
            return await _context.categories
                .OrderBy(c => c.categoryname)
                .ToListAsync();
        }

        // 6. Получение ресурсов (дорожек, столов) для услуги
        public async Task<List<Resources>> GetServiceResourcesAsync(int serviceId)
        {
            return await _context.resources
                .Where(r => r.serviceid == serviceId)
                .OrderBy(r => r.name)
                .ToListAsync();
        }

        // 7. Получение популярных услуг (по количеству заказов)
        public async Task<List<ServiceWithStats>> GetPopularServicesAsync(int topCount = 5)
        {
            return await _context.services
                .Include(s => s.resources)
                .Select(s => new ServiceWithStats
                {
                    Service = s,
                    OrderCount = s.orders.Count(o => o.status != "отменен"),
                    TotalRevenue = s.orders
                        .Where(o => o.status != "отменен")
                        .Sum(o => o.totalprice)
                })
                .OrderByDescending(s => s.OrderCount)
                .Take(topCount)
                .ToListAsync();
        }

        // 8. Получение дорогих услуг (по цене)
        public async Task<List<Services>> GetExpensiveServicesAsync(decimal minPrice)
        {
            return await _context.services
                .Include(s => s.resources)
                .Where(s => s.weekdayprice >= minPrice || s.weekendprice >= minPrice)
                .OrderByDescending(s => Math.Max(s.weekdayprice, s.weekendprice))
                .ToListAsync();
        }

        // 9. Проверка доступности услуги на дату
        public async Task<bool> IsServiceAvailableAsync(int serviceId, DateTime date)
        {
            var service = await _context.services.FindAsync(serviceId);
            if (service == null) return false;

            int dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            TimeSpan requestTime = date.TimeOfDay;
            if (requestTime < service.starttime || requestTime > service.endtime)
                return false;

            return true;
        }

        // 10. Получение услуг с фильтрами
        public async Task<List<Services>> GetFilteredServicesAsync(
            int? categoryId = null,
            string searchTerm = null,
            decimal? maxPrice = null,
            int? minDuration = null)
        {
            var query = _context.services
                .Include(s => s.resources)
                .Include(s => s.servicecategories)
                    .ThenInclude(sc => sc.category)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(s => s.servicecategories.Any(sc => sc.categoryid == categoryId.Value));

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(s => s.name.Contains(searchTerm));

            if (maxPrice.HasValue)
                query = query.Where(s => s.weekdayprice <= maxPrice.Value && s.weekendprice <= maxPrice.Value);

            if (minDuration.HasValue)
                query = query.Where(s => s.duration >= minDuration.Value);

            return await query.ToListAsync();
        }


        public class ServiceWithStats
        {
            public Services Service { get; set; }
            public int OrderCount { get; set; }
            public decimal TotalRevenue { get; set; }
        }
    }
}