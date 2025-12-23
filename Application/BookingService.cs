using Microsoft.EntityFrameworkCore;
using DAL.Entities;
using System.Transactions;
using DAL;
using BLL.Models;
using ServiceEntity = DAL.Entities.Services;

namespace BLL
{
    public class BookingService
    {
        private readonly AppDbContext _context;
        private readonly UserService _userService;

        public BookingService(AppDbContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }


        // 1. Проверить доступность услуги на дату/время
        public async Task<bool> CheckServiceAvailabilityAsync(int serviceId, DateTime dateTime, int durationMinutes)
        {
            var service = await _context.services.FindAsync(serviceId);
            if (service == null)
                return false;

            var time = dateTime.TimeOfDay;
            if (time < service.starttime || time > service.endtime)
                return false;

            var endTime = dateTime.AddMinutes(durationMinutes);
            if (endTime.TimeOfDay > service.endtime)
                return false;

            return true;
        }

        // 2. Найти свободные ресурсы на дату/время
        public async Task<List<Resources>> FindAvailableResourcesAsync(int serviceId, DateTime dateTime,
            int durationMinutes, int peopleCount)
        {
            var service = await _context.services
                .Include(s => s.resources)
                .FirstOrDefaultAsync(s => s.serviceid == serviceId);

            if (service == null)
                return new List<Resources>();

            var allServiceResources = service.resources.ToList();

            var availableResources = new List<Resources>();
            var endTime = dateTime.AddMinutes(durationMinutes);

            foreach (var resource in allServiceResources)
            {
                var isBusy = await IsResourceBusyAsync(resource.resourceid, dateTime, endTime);
                if (!isBusy)
                    availableResources.Add(resource);
            }

            return availableResources;
        }

        // 3. Проверить занят ли ресурс в указанный период
        private async Task<bool> IsResourceBusyAsync(int resourceId, DateTime startTime, DateTime endTime)
        {
            return await _context.orderresources
                .Include(or => or.order)
                    .ThenInclude(o => o.service)
                .Where(or => or.resourceid == resourceId)
                .Where(or => or.order.status != "отменен")
                .AnyAsync(or =>
                    (startTime < or.order.orderdate.AddMinutes(or.order.service.duration) &&
                     endTime > or.order.orderdate));
        }

        // 4. Создать новый заказ
        public async Task<Orders?> CreateBookingAsync(BookingRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (request.PeopleCount <= 0)
                    throw new Exception("Количество человек должно быть больше 0");

                if (request.SelectedResourceIds == null || !request.SelectedResourceIds.Any())
                    throw new Exception("Не выбраны ресурсы");

                var availableResources = await FindAvailableResourcesAsync(
                    request.ServiceId, request.BookingDateTime,
                    request.DurationMinutes, request.PeopleCount);

                var selectedResources = availableResources
                    .Where(r => request.SelectedResourceIds.Contains(r.resourceid))
                    .ToList();

                if (selectedResources.Count != request.SelectedResourceIds.Count)
                {
                    var missingIds = request.SelectedResourceIds
                        .Where(id => !selectedResources.Any(r => r.resourceid == id))
                        .ToList();
                    throw new Exception($"Ресурсы с ID {string.Join(", ", missingIds)} недоступны или уже заняты");
                }

                var service = await _context.services.FindAsync(request.ServiceId);
                if (service == null)
                    throw new Exception("Услуга не найдена");

                // Рассчитываем базовую цену услуги
                var servicePrice = CalculateBasePrice(service, request.BookingDateTime, request.DurationMinutes);
                
                // Добавляем стоимость дополнительных услуг
                decimal extrasPrice = 0;
                if (request.IncludeInstructor)
                    extrasPrice += 500;
                if (request.IncludeEquipment)
                    extrasPrice += 300; // Предполагаемая стоимость оборудования
                if (request.IncludeFood)
                    extrasPrice += 1000;
                
                var basePrice = servicePrice + extrasPrice;
                var finalPrice = await ApplyUserDiscountAsync(request.UserId, basePrice);

                var order = new Orders
                {
                    userid = request.UserId,
                    serviceid = request.ServiceId,
                    orderdate = request.BookingDateTime,
                    peoplecount = request.PeopleCount,
                    totalprice = finalPrice,
                    status = "создан",
                    created_at = DateTime.UtcNow
                };

                _context.orders.Add(order);
                await _context.SaveChangesAsync();

                foreach (var resourceId in request.SelectedResourceIds)
                {
                    _context.orderresources.Add(new Orderresources
                    {
                        orderid = order.orderid,
                        resourceid = resourceId
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetOrderDetailsAsync(order.orderid);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                System.Diagnostics.Debug.WriteLine($"Ошибка создания брони: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"InnerException: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        // 5. Расчет базовой цены
        private decimal CalculateBasePrice(ServiceEntity service, DateTime bookingDateTime, int durationMinutes)
        {
            var isWeekend = bookingDateTime.DayOfWeek == DayOfWeek.Saturday ||
                           bookingDateTime.DayOfWeek == DayOfWeek.Sunday;

            var pricePerHour = isWeekend ? service.weekendprice : service.weekdayprice;
            var hours = durationMinutes / 60.0m;

            return pricePerHour * hours;
        }

        // 6. Применить скидку пользователя
        private async Task<decimal> ApplyUserDiscountAsync(int userId, decimal price)
        {
            try
            {
                var discountPercent = await _userService.GetUserDiscountAsync(userId);
                var discountDecimal = discountPercent / 100m;
                return _userService.ApplyDiscount(price, discountDecimal);
            }
            catch
            {
                return price;
            }
        }

        // 7. Получить все заказы пользователя (клиент)
        public async Task<List<Orders>> GetUserOrdersAsync(int userId)
        {
            return await _context.orders
                .Where(o => o.userid == userId)
                .Include(o => o.service)
                .Include(o => o.orderresources)
                    .ThenInclude(or => or.resource)
                .OrderByDescending(o => o.orderdate)
                .ToListAsync();
        }

        // 8. Получить детали конкретного заказа
        public async Task<Orders?> GetOrderDetailsAsync(int orderId)
        {
            return await _context.orders
                .Include(o => o.user)
                .Include(o => o.service)
                .Include(o => o.orderresources)
                    .ThenInclude(or => or.resource)
                .FirstOrDefaultAsync(o => o.orderid == orderId);
        }

        // 9. Получить все заказы
        public async Task<List<Orders>> GetAllOrdersAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.orders
                .Include(o => o.user)
                .Include(o => o.service)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.orderdate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.orderdate <= endDate.Value);

            return await query
                .OrderByDescending(o => o.orderdate)
                .ToListAsync();
        }

        // 10. Отменить заказ 
        public async Task<bool> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.orders.FindAsync(orderId);
            if (order == null || order.userid != userId)
                return false;

            var cancellableStatuses = new[] { "создан", "ожидает оплаты" };
            if (!cancellableStatuses.Contains(order.status))
                return false;

            order.status = "отменен";
            await _context.SaveChangesAsync();
            return true;
        }

        // 11. Изменить статус заказа
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            var validStatuses = new[] { "создан", "ожидает оплаты", "оплачен", "подтвержден", "завершен", "отменен" };

            if (!validStatuses.Contains(newStatus))
                return false;

            var order = await _context.orders.FindAsync(orderId);
            if (order == null)
                return false;

            order.status = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        // 11a. Обработать успешную оплату заказа
        public async Task<bool> ProcessPaymentSuccessAsync(int orderId)
        {
            var order = await _context.orders.FindAsync(orderId);
            if (order == null)
                return false;

            order.status = "оплачен";
            await _context.SaveChangesAsync();
            return true;
        }

        // 12. Изменить время заказа
        public async Task<bool> RescheduleOrderAsync(int orderId, DateTime newDateTime)
        {
            var order = await _context.orders
                .Include(o => o.service)
                .Include(o => o.orderresources)
                .FirstOrDefaultAsync(o => o.orderid == orderId);

            if (order == null || order.status == "отменен")
                return false;

            var resourceIds = order.orderresources.Select(or => or.resourceid).ToList();
            var endTime = newDateTime.AddMinutes(order.service.duration);

            foreach (var resourceId in resourceIds)
            {
                var isBusy = await IsResourceBusyDuringRescheduleAsync(resourceId, orderId, newDateTime, endTime);
                if (isBusy)
                    return false;
            }

            order.orderdate = newDateTime;
            await _context.SaveChangesAsync();

            return true;
        }

        // 13. Проверка ресурса при переносе (исключая текущий заказ)
        private async Task<bool> IsResourceBusyDuringRescheduleAsync(int resourceId, int excludeOrderId,
            DateTime startTime, DateTime endTime)
        {
            return await _context.orderresources
                .Include(or => or.order)
                    .ThenInclude(o => o.service)
                .Where(or => or.resourceid == resourceId && or.order.orderid != excludeOrderId)
                .Where(or => or.order.status != "отменен")
                .AnyAsync(or =>
                    (startTime < or.order.orderdate.AddMinutes(or.order.service.duration) &&
                     endTime > or.order.orderdate));
        }

        // 14. Получить статистику по заказам
        public async Task<BookingStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.orders
                .Where(o => o.orderdate >= startDate && o.orderdate <= endDate)
                .Include(o => o.service)
                .ToListAsync();

            return new BookingStatistics
            {
                TotalOrders = orders.Count,
                TotalRevenue = orders.Sum(o => o.totalprice),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.totalprice) : 0,
                OrdersByStatus = orders.GroupBy(o => o.status)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RevenueByService = orders.GroupBy(o => o.service.name)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.totalprice))
            };
        }

        public async Task<List<ResourceModel>> FindAvailableResourceModelsAsync(int serviceId, DateTime dateTime,
            int durationMinutes, int peopleCount)
        {
            var resources = await FindAvailableResourcesAsync(serviceId, dateTime, durationMinutes, peopleCount);

            return resources.Select(r => new ResourceModel
            {
                Id = r.resourceid,
                Name = r.name,
                Capacity = r.capacity
            }).ToList();
        }

        public class BookingRequest
        {
            public int UserId { get; set; }
            public int ServiceId { get; set; }
            public DateTime BookingDateTime { get; set; }
            public int DurationMinutes { get; set; }
            public int PeopleCount { get; set; }
            public List<int> SelectedResourceIds { get; set; } = new();
            public bool IncludeInstructor { get; set; }
            public bool IncludeEquipment { get; set; }
            public bool IncludeFood { get; set; }
        }

        public class BookingStatistics
        {
            public int TotalOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal AverageOrderValue { get; set; }
            public Dictionary<string, int> OrdersByStatus { get; set; } = new();
            public Dictionary<string, decimal> RevenueByService { get; set; } = new();
        }
    }
}
