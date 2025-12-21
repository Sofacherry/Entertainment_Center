using Microsoft.EntityFrameworkCore;
using DAL.Entities;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using DAL;
using Npgsql.Internal;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace BLL
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        // 1. Популярные услуги (по количеству заказов)
        public async Task<List<PopularServiceReport>> GetPopularServicesAsync(
            DateTime? startDate = null, DateTime? endDate = null, int topCount = 10)
        {
            var query = _context.orders
                .Where(o => o.status != "отменен")
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.orderdate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(o => o.orderdate <= endDate.Value);

            return await query
                .GroupBy(o => o.serviceid)
                .Select(g => new PopularServiceReport
                {
                    ServiceId = g.Key,
                    ServiceName = g.First().service.name,
                    OrderCount = g.Count(),
                    TotalRevenue = g.Sum(o => o.totalprice),
                    AveragePeople = g.Average(o => o.peoplecount),
                    PopularityRank = 0 
                })
                .OrderByDescending(r => r.OrderCount)
                .Take(topCount)
                .ToListAsync();
        }

        // 2. Дорогие услуги (по цене)
        public async Task<List<ExpensiveServiceReport>> GetExpensiveServicesAsync(
            decimal? minPrice = null, int topCount = 10)
        {
            var minPriceValue = minPrice ?? await GetAverageServicePriceAsync() * 1.5m;

            return await _context.services
                .Select(s => new ExpensiveServiceReport
                {
                    ServiceId = s.serviceid,
                    ServiceName = s.name,
                    Duration = s.duration,
                    WeekdayPrice = s.weekdayprice,
                    WeekendPrice = s.weekendprice,
                    AveragePrice = (s.weekdayprice + s.weekendprice) / 2,
                    PriceCategory = GetPriceCategory(s.weekdayprice, s.weekendprice)
                })
                .Where(r => r.AveragePrice >= minPriceValue)
                .OrderByDescending(r => r.AveragePrice)
                .Take(topCount)
                .ToListAsync();
        }

        // 3. Средняя цена услуг (вспомогательный метод)
        private async Task<decimal> GetAverageServicePriceAsync()
        {
            var prices = await _context.services
                .Select(s => (s.weekdayprice + s.weekendprice) / 2)
                .ToListAsync();

            return prices.Any() ? prices.Average() : 0;
        }

        // 4. Отчет о выручке за период
        public async Task<RevenueReport> GetRevenueReportAsync(
            DateTime startDate, DateTime endDate)
        {
            var orders = await _context.orders
                .Where(o => o.orderdate >= startDate && o.orderdate <= endDate)
                .Include(o => o.service)
                .Include(o => o.user)
                .ToListAsync();

            var completedOrders = orders.Where(o => o.status != "отменен").ToList();

            return new RevenueReport
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalOrders = orders.Count,
                CompletedOrders = completedOrders.Count,
                CancelledOrders = orders.Count - completedOrders.Count,
                TotalRevenue = completedOrders.Sum(o => o.totalprice),
                AverageOrderValue = completedOrders.Any() ?
                    completedOrders.Average(o => o.totalprice) : 0,

                RevenueByService = completedOrders
                    .GroupBy(o => o.service.name)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.totalprice)),

                RevenueByDay = completedOrders
                    .GroupBy(o => o.orderdate.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Sum(o => o.totalprice)),

                TopServices = completedOrders
                    .GroupBy(o => o.service.name)
                    .Select(g => new ServiceRevenue
                    {
                        ServiceName = g.Key,
                        Revenue = g.Sum(o => o.totalprice),
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(s => s.Revenue)
                    .Take(5)
                    .ToList(),

                CustomerStatistics = completedOrders
                    .GroupBy(o => o.userid)
                    .Select(g => new CustomerStats
                    {
                        UserId = g.Key,
                        UserName = g.First().user?.name ?? "Неизвестно",
                        OrderCount = g.Count(),
                        TotalSpent = g.Sum(o => o.totalprice)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(10)
                    .ToList()
            };
        }

        // 5. Ежедневный отчет
        public async Task<DailyReport> GetDailyReportAsync(DateTime date)
        {
            var orders = await _context.orders
                .Where(o => o.orderdate.Date == date.Date)
                .Include(o => o.service)
                .Include(o => o.user)
                .ToListAsync();

            var resources = await _context.orderresources
                .Where(or => or.order.orderdate.Date == date.Date)
                .Include(or => or.resource)
                .Include(or => or.order)
                .ToListAsync();

            return new DailyReport
            {
                ReportDate = date,
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.status != "отменен"),

                OrdersByStatus = orders
                    .GroupBy(o => o.status)
                    .ToDictionary(g => g.Key, g => g.Count()),

                OrdersByHour = orders
                    .Where(o => o.status != "отменен")
                    .GroupBy(o => o.orderdate.Hour)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count()),

                ResourceUtilization = resources
                    .Where(or => or.order.status != "отменен")
                    .GroupBy(or => or.resource.name)
                    .ToDictionary(g => g.Key, g => g.Count()),

                BusiestHour = orders
                    .Where(o => o.status != "отменен")
                    .GroupBy(o => o.orderdate.Hour)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault()
            };
        }

        // 6. Статистика по ресурсам
        public async Task<ResourceReport> GetResourceReportAsync(
            DateTime startDate, DateTime endDate)
        {
            var resourceUsage = await _context.orderresources
                .Where(or => or.order.orderdate >= startDate &&
                           or.order.orderdate <= endDate &&
                           or.order.status != "отменен")
                .Include(or => or.resource)
                .Include(or => or.order)
                .GroupBy(or => or.resource)
                .Select(g => new ResourceUsage
                {
                    ResourceId = g.Key.resourceid,
                    ResourceName = g.Key.name,
                    Capacity = g.Key.capacity,
                    UsageCount = g.Count(),
                    TotalHours = g.Sum(or => or.order.service.duration) / 60.0m,
                    UtilizationRate = 0 
                })
                .ToListAsync();

            // Рассчитываем коэффициент использования
            var totalDays = (endDate - startDate).Days + 1;
            var maxPossibleUsage = totalDays * 8; 

            foreach (var usage in resourceUsage)
            {
                usage.UtilizationRate = maxPossibleUsage > 0 ?
                    usage.TotalHours / maxPossibleUsage : 0;
            }

            return new ResourceReport
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                ResourceUsages = resourceUsage.OrderByDescending(r => r.UtilizationRate).ToList(),
                MostUsedResource = resourceUsage.OrderByDescending(r => r.UsageCount).FirstOrDefault(),
                LeastUsedResource = resourceUsage.OrderBy(r => r.UsageCount).FirstOrDefault()
            };
        }

        // 7. Экспорт отчета о выручке в PDF
        public async Task<byte[]> ExportRevenueReportToPdfAsync(
            DateTime startDate, DateTime endDate)
        {
            var report = await GetRevenueReportAsync(startDate, endDate);

            using var memoryStream = new MemoryStream();
            var document = new iTextSharp.text.Document(
                iTextSharp.text.PageSize.A4, 50, 50, 50, 50);
            var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Заголовок
            var titleFont = iTextSharp.text.FontFactory.GetFont(
                "Arial", 18, iTextSharp.text.Font.BOLD);
            var title = new iTextSharp.text.Paragraph("Отчет о выручке", titleFont)
            {
                Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            // Период
            var periodFont = iTextSharp.text.FontFactory.GetFont("Arial", 12);
            var period = new iTextSharp.text.Paragraph(
                $"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}", periodFont)
            {
                SpacingAfter = 10
            };
            document.Add(period);

            // Основная статистика
            var statsTable = new iTextSharp.text.pdf.PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingBefore = 10,
                SpacingAfter = 20
            };

            AddStatRow(statsTable, "Всего заказов:", report.TotalOrders.ToString());
            AddStatRow(statsTable, "Завершенных заказов:", report.CompletedOrders.ToString());
            AddStatRow(statsTable, "Отмененных заказов:", report.CancelledOrders.ToString());
            AddStatRow(statsTable, "Общая выручка:", $"{report.TotalRevenue:N2} руб.");
            AddStatRow(statsTable, "Средний чек:", $"{report.AverageOrderValue:N2} руб.");

            document.Add(statsTable);

            // Топ услуг
            if (report.TopServices.Any())
            {
                var servicesTitle = new iTextSharp.text.Paragraph("Топ услуг по выручке", titleFont)
                {
                    SpacingBefore = 20,
                    SpacingAfter = 10
                };
                document.Add(servicesTitle);

                var servicesTable = new iTextSharp.text.pdf.PdfPTable(3)
                {
                    WidthPercentage = 100,
                    SpacingAfter = 20
                };

                servicesTable.AddCell("Услуга");
                servicesTable.AddCell("Количество заказов");
                servicesTable.AddCell("Выручка");

                foreach (var service in report.TopServices)
                {
                    servicesTable.AddCell(service.ServiceName);
                    servicesTable.AddCell(service.OrderCount.ToString());
                    servicesTable.AddCell($"{service.Revenue:N2} руб.");
                }

                document.Add(servicesTable);
            }

            // Топ клиентов
            if (report.CustomerStatistics.Any())
            {
                var customersTitle = new iTextSharp.text.Paragraph("Топ клиентов", titleFont)
                {
                    SpacingBefore = 20,
                    SpacingAfter = 10
                };
                document.Add(customersTitle);

                var customersTable = new iTextSharp.text.pdf.PdfPTable(3)
                {
                    WidthPercentage = 100
                };

                customersTable.AddCell("Клиент");
                customersTable.AddCell("Количество заказов");
                customersTable.AddCell("Общая сумма");

                foreach (var customer in report.CustomerStatistics)
                {
                    customersTable.AddCell(customer.UserName);
                    customersTable.AddCell(customer.OrderCount.ToString());
                    customersTable.AddCell($"{customer.TotalSpent:N2} руб.");
                }

                document.Add(customersTable);
            }

            // Дата генерации
            var dateFont = iTextSharp.text.FontFactory.GetFont(
                "Arial", 10, iTextSharp.text.Font.ITALIC);
            var generationDate = new iTextSharp.text.Paragraph(
                $"Отчет сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm}", dateFont)
            {
                Alignment = iTextSharp.text.Element.ALIGN_RIGHT,
                SpacingBefore = 30
            };
            document.Add(generationDate);

            document.Close();

            return memoryStream.ToArray();
        }

        // 8. Экспорт ежедневного отчета в PDF
        public async Task<byte[]> ExportDailyReportToPdfAsync(DateTime date)
        {
            var report = await GetDailyReportAsync(date);

            using var memoryStream = new MemoryStream();
            var document = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);
            var writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            var titleFont = iTextSharp.text.FontFactory.GetFont(
                "Arial", 16, iTextSharp.text.Font.BOLD);
            var title = new iTextSharp.text.Paragraph(
                $"Ежедневный отчет за {date:dd.MM.yyyy}", titleFont)
            {
                Alignment = iTextSharp.text.Element.ALIGN_CENTER,
                SpacingAfter = 20
            };
            document.Add(title);

            document.Close();
            return memoryStream.ToArray();
        }

        private void AddStatRow(PdfPTable table, string label, string value)
        {
            var labelCell = new PdfPCell(new Phrase(label))
            {
                Border = 0,
                Padding = 5
            };

            var valueCell = new PdfPCell(new Phrase(value))
            {
                Border = 0,
                Padding = 5,
                HorizontalAlignment = Element.ALIGN_RIGHT
            };

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        private string GetPriceCategory(decimal weekdayPrice, decimal weekendPrice)
        {
            var avgPrice = (weekdayPrice + weekendPrice) / 2;

            return avgPrice switch
            {
                < 1000 => "Бюджетная",
                < 3000 => "Средняя",
                < 5000 => "Дорогая",
                _ => "Премиум"
            };
        }
        public class PopularServiceReport
        {
            public int ServiceId { get; set; }
            public string ServiceName { get; set; }
            public int OrderCount { get; set; }
            public decimal TotalRevenue { get; set; }
            public double AveragePeople { get; set; }
            public int PopularityRank { get; set; }
        }

        public class ExpensiveServiceReport
        {
            public int ServiceId { get; set; }
            public string ServiceName { get; set; }
            public int Duration { get; set; }
            public decimal WeekdayPrice { get; set; }
            public decimal WeekendPrice { get; set; }
            public decimal AveragePrice { get; set; }
            public string PriceCategory { get; set; }
        }

        public class RevenueReport
        {
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
            public int TotalOrders { get; set; }
            public int CompletedOrders { get; set; }
            public int CancelledOrders { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal AverageOrderValue { get; set; }
            public Dictionary<string, decimal> RevenueByService { get; set; } = new();
            public Dictionary<DateTime, decimal> RevenueByDay { get; set; } = new();
            public List<ServiceRevenue> TopServices { get; set; } = new();
            public List<CustomerStats> CustomerStatistics { get; set; } = new();
        }

        public class DailyReport
        {
            public DateTime ReportDate { get; set; }
            public int TotalOrders { get; set; }
            public int CompletedOrders { get; set; }
            public Dictionary<string, int> OrdersByStatus { get; set; } = new();
            public Dictionary<int, int> OrdersByHour { get; set; } = new();
            public Dictionary<string, int> ResourceUtilization { get; set; } = new();
            public int BusiestHour { get; set; }
        }

        public class ResourceReport
        {
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
            public List<ResourceUsage> ResourceUsages { get; set; } = new();
            public ResourceUsage MostUsedResource { get; set; }
            public ResourceUsage LeastUsedResource { get; set; }
        }

        public class ServiceRevenue
        {
            public string ServiceName { get; set; }
            public decimal Revenue { get; set; }
            public int OrderCount { get; set; }
        }

        public class CustomerStats
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public int OrderCount { get; set; }
            public decimal TotalSpent { get; set; }
        }

        public class ResourceUsage
        {
            public int ResourceId { get; set; }
            public string ResourceName { get; set; }
            public int Capacity { get; set; }
            public int UsageCount { get; set; }
            public decimal TotalHours { get; set; }
            public decimal UtilizationRate { get; set; }
        }
    }
}