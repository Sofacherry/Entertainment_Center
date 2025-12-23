using Microsoft.EntityFrameworkCore;

// Пояснение по "популярным услугам":
// Если не считает правильно популярные услуги, вероятно, ошибка в том,
// что учитываются не все заказы (например, включены отменённые или не оплаченные).
// Проверьте, чтобы при подсчёте популярных услуг фильтровались только завершённые/оплаченные заказы!
// Например, используйте только те заказы, где o.status != "отменен". 
// Также убедитесь, что группировка и подсчёт выполняются по id или названию услуги корректно.

// Пример запроса:
// var servicePopularity = await _context.orderItems
//      .Where(oi => oi.order.status != "отменен")
//      .GroupBy(oi => oi.serviceId)
//      .Select(g => new { ServiceId = g.Key, Count = g.Count() })
//      .OrderByDescending(x => x.Count)
//      .ToListAsync();

using DAL.Entities;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BLL.Models;

namespace BLL
{
    public class ReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        // Получить все завершенные заказы (все кроме отмененных)
        private IQueryable<Orders> GetCompletedOrdersQuery()
        {
            return _context.orders
                .Include(o => o.service)
                .Include(o => o.user)
                .Where(o => o.status != "отменен");
        }

        // Получить оплаченные заказы (включаем статус "создан" для демонстрации)
        private IQueryable<Orders> GetPaidOrdersQuery()
        {
            // Для демонстрации считаем ВСЕ заказы, кроме отмененных
            return _context.orders
                .Where(o => o.status != "отменен");
            // Если нужны только оплаченные, то так:
            // .Where(o => o.status == "оплачен" || o.status == "подтвержден");
        }

        // 1. Дашборд - все метрики из БД
        public async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            var metrics = new DashboardMetrics();
            var today = DateTime.Today;

            try
            {
                // Загружаем все заказы с включением связанных данных
                var allOrders = await _context.orders
                    .Include(o => o.service)
                    .Include(o => o.user)
                    .ToListAsync();
                
                // ДЛЯ ДЕМОНСТРАЦИИ: считаем ВСЕ заказы кроме отмененных как оплаченные
                var paidOrders = allOrders.Where(o => o.status != "отменен").ToList();

                Console.WriteLine($"Всего заказов в БД: {allOrders.Count}");
                Console.WriteLine($"Заказов для выручки (не отмененные): {paidOrders.Count}");

                // Выводим детальную информацию о заказах
                foreach (var order in allOrders)
                {
                    Console.WriteLine($"Заказ #{order.orderid}: Статус='{order.status}', Цена={order.totalprice}, Дата={order.orderdate}");
                }

                // Общая выручка (все кроме отмененных)
                metrics.TotalRevenue = paidOrders.Sum(o => o.totalprice);
                Console.WriteLine($"Общая выручка: {metrics.TotalRevenue}");

                // Всего заказов
                metrics.TotalOrders = allOrders.Count;

                // Заказы сегодня (сравниваем даты без учета времени)
                var todayOrders = allOrders.Where(o => o.orderdate.Date == today.Date).ToList();
                metrics.TodayOrders = todayOrders.Count;

                // Выручка сегодня (все кроме отмененных)
                var completedTodayOrders = todayOrders.Where(o => o.status != "отменен").ToList();
                metrics.TodayRevenue = completedTodayOrders.Sum(o => o.totalprice);
                Console.WriteLine($"Выручка сегодня: {metrics.TodayRevenue}");

                // Активные пользователи (с заказами за последние 30 дней)
                var last30Days = DateTime.UtcNow.AddDays(-30);
                metrics.ActiveUsers = allOrders
                    .Where(o => o.orderdate >= last30Days)
                    .Select(o => o.userid)
                    .Distinct()
                    .Count();

                // Посещения за месяц (все заказы кроме отмененных)
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                metrics.VisitStatistics = allOrders
                    .Count(o => o.orderdate >= firstDayOfMonth && o.status != "отменен");

                // Самая популярная услуга (по количеству заказов - все кроме отмененных)
                var completedOrders = allOrders.Where(o => o.status != "отменен").ToList();
                var popularServiceData = completedOrders
                    .GroupBy(o => o.serviceid)
                    .Select(g => new
                    {
                        ServiceId = g.Key,
                        ServiceName = g.First().service?.name ?? "Неизвестно",
                        OrderCount = g.Count(),
                        // ДЛЯ ДЕМОНСТРАЦИИ: считаем выручку для всех заказов в группе
                        TotalRevenue = g.Sum(o => o.totalprice)
                    })
                    .OrderByDescending(x => x.OrderCount)
                    .FirstOrDefault();

                if (popularServiceData != null)
                {
                    metrics.MostPopularService = popularServiceData.ServiceName;
                    metrics.MostPopularServiceCount = popularServiceData.OrderCount;
                    metrics.MostPopularServiceRevenue = popularServiceData.TotalRevenue;
                    Console.WriteLine($"Популярная услуга: {popularServiceData.ServiceName}, Выручка: {popularServiceData.TotalRevenue}");
                }

                // Самый популярный месяц для развлечений (все заказы кроме отмененных)
                var popularMonthData = completedOrders
                    .GroupBy(o => new { o.orderdate.Year, o.orderdate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        OrderCount = g.Count(),
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy")
                    })
                    .OrderByDescending(x => x.OrderCount)
                    .FirstOrDefault();

                if (popularMonthData != null)
                {
                    metrics.MostPopularMonth = popularMonthData.MonthName;
                    metrics.MostPopularMonthOrders = popularMonthData.OrderCount;
                }

                // График выручки по дням (последние 7 дней, все кроме отмененных)
                var last7Days = DateTime.Today.AddDays(-6);
                var revenueByDayData = completedOrders
                    .Where(o => o.orderdate >= last7Days)
                    .GroupBy(o => o.orderdate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.totalprice)
                    })
                    .ToList();

                // Заполняем все дни
                for (int i = 0; i < 7; i++)
                {
                    var date = today.AddDays(-i);
                    var revenueData = revenueByDayData.FirstOrDefault(r => r.Date.Date == date);
                    metrics.Last7DaysRevenue[date] = revenueData?.Revenue ?? 0m;
                }

                // Логируем данные для графика
                Console.WriteLine("Данные для графика:");
                foreach (var item in metrics.Last7DaysRevenue)
                {
                    Console.WriteLine($"{item.Key:dd.MM.yyyy}: {item.Value} ₽");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading dashboard metrics: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }

            return metrics;
        }

        // 2. Популярные услуги (топ-5) - все кроме отмененных
        public async Task<List<PopularServiceReport>> GetPopularServicesAsync(
            DateTime? startDate = null, DateTime? endDate = null, int topCount = 5)
        {
            try
            {
                // Загружаем все данные с включением
                var allOrders = await _context.orders
                    .Include(o => o.service)
                    .ToListAsync();

                Console.WriteLine($"=== GetPopularServicesAsync ===");
                Console.WriteLine($"Всего заказов: {allOrders.Count}");
                Console.WriteLine($"Статусы заказов: {string.Join(", ", allOrders.Select(o => $"{o.orderid}:{o.status}"))}");

                // Фильтруем заказы
                var filteredOrders = allOrders
                    .Where(o => o.status != "отменен")
                    .ToList();

                if (startDate.HasValue)
                {
                    var startDateUtc = startDate.Value.Kind == DateTimeKind.Local ?
                        startDate.Value.ToUniversalTime() : startDate.Value;
                    filteredOrders = filteredOrders.Where(o => o.orderdate >= startDateUtc).ToList();
                }

                if (endDate.HasValue)
                {
                    var endDateUtc = endDate.Value.Kind == DateTimeKind.Local ?
                        endDate.Value.ToUniversalTime() : endDate.Value;
                    filteredOrders = filteredOrders.Where(o => o.orderdate <= endDateUtc).ToList();
                }

                Console.WriteLine($"Заказов после фильтрации: {filteredOrders.Count}");

                // Группируем услуги
                var groupedResults = filteredOrders
                    .GroupBy(o => new
                    {
                        o.serviceid,
                        ServiceName = o.service?.name ?? "Неизвестно"
                    })
                    .Select(g => new
                    {
                        ServiceId = g.Key.serviceid,
                        ServiceName = g.Key.ServiceName,
                        OrderCount = g.Count(),
                        // ДЛЯ ДЕМОНСТРАЦИИ: считаем выручку для всех заказов
                        TotalRevenue = g.Sum(o => o.totalprice),
                        AveragePeople = g.Average(o => (double?)o.peoplecount)
                    })
                    .Where(r => r.OrderCount > 0)
                    .OrderByDescending(r => r.OrderCount)
                    .Take(topCount)
                    .ToList();

                Console.WriteLine($"Найдено услуг: {groupedResults.Count}");
                foreach (var r in groupedResults)
                {
                    Console.WriteLine($"Услуга: {r.ServiceName}, Заказов: {r.OrderCount}, Выручка: {r.TotalRevenue}");
                }

                var rank = 1;
                var result = groupedResults.Select(r => new PopularServiceReport
                {
                    ServiceId = r.ServiceId,
                    ServiceName = r.ServiceName,
                    OrderCount = r.OrderCount,
                    TotalRevenue = r.TotalRevenue,
                    AveragePeople = r.AveragePeople ?? 0.0,
                    PopularityRank = rank++
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading popular services: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new List<PopularServiceReport>();
            }
        }

        // 3. Дорогие услуги (фильтр по цене)
        public async Task<List<ExpensiveServiceReport>> GetExpensiveServicesAsync(
            decimal? minPrice = null, int topCount = 10)
        {
            try
            {
                var query = _context.services.AsQueryable();

                if (minPrice.HasValue && minPrice > 0)
                {
                    query = query.Where(s => s.weekdayprice >= minPrice.Value || s.weekendprice >= minPrice.Value);
                }

                var results = await query
                    .Select(s => new
                    {
                        s.serviceid,
                        s.name,
                        s.duration,
                        s.weekdayprice,
                        s.weekendprice
                    })
                    .OrderByDescending(s => Math.Max(s.weekdayprice, s.weekendprice))
                    .Take(topCount)
                    .ToListAsync();

                Console.WriteLine($"Найдено дорогих услуг: {results.Count}");

                return results.Select(s => new ExpensiveServiceReport
                {
                    ServiceId = s.serviceid,
                    ServiceName = s.name,
                    Duration = s.duration,
                    WeekdayPrice = s.weekdayprice,
                    WeekendPrice = s.weekendprice,
                    AveragePrice = (s.weekdayprice + s.weekendprice) / 2,
                    PriceCategory = GetPriceCategory(s.weekdayprice, s.weekendprice)
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading expensive services: {ex.Message}");
                return new List<ExpensiveServiceReport>();
            }
        }

        // 4. Отчет о выручке за период (только оплаченные заказы) - ИСПРАВЛЕННЫЙ
        // 4. Отчет о выручке за период (все кроме отмененных) - ИСПРАВЛЕННЫЙ
        public async Task<RevenueReport> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine($"=== GetRevenueReportAsync ===");
                Console.WriteLine($"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");

                // Загружаем все заказы
                var allOrders = await _context.orders
                    .Include(o => o.service)
                    .Include(o => o.user)
                    .ToListAsync();

                Console.WriteLine($"Всего заказов в БД: {allOrders.Count}");

                // Фильтруем по дате
                allOrders = allOrders
                    .Where(o => o.orderdate >= startDate && o.orderdate <= endDate)
                    .ToList();

                Console.WriteLine($"Заказов в периоде: {allOrders.Count}");

                // Показываем все заказы с деталями
                foreach (var order in allOrders)
                {
                    Console.WriteLine($"Заказ #{order.orderid}: Услуга={order.service?.name}, Цена={order.totalprice}, Статус={order.status}");
                }

                // Для выручки считаем ВСЕ заказы кроме отмененных
                var paidOrders = allOrders
                    .Where(o => o.status != "отменен")
                    .ToList();

                Console.WriteLine($"Заказов для выручки (не отмененные): {paidOrders.Count}");

                // Отмененные заказы
                var cancelledOrders = allOrders.Where(o => o.status == "отменен").ToList();
                Console.WriteLine($"Отмененных заказов: {cancelledOrders.Count}");

                // Подсчитываем выручку
                var totalRevenue = paidOrders.Sum(o => o.totalprice);
                var averageOrderValue = paidOrders.Any() ? paidOrders.Average(o => o.totalprice) : 0m;

                Console.WriteLine($"Общая выручка: {totalRevenue}");
                Console.WriteLine($"Средний чек: {averageOrderValue}");

                // Выручка по дням
                var revenueByDay = new Dictionary<DateTime, decimal>();
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    var dayRevenue = paidOrders
                        .Where(o => o.orderdate.Date == currentDate)
                        .Sum(o => o.totalprice);
                    revenueByDay[currentDate] = dayRevenue;
                    Console.WriteLine($"{currentDate:dd.MM.yyyy}: {dayRevenue} ₽");
                    currentDate = currentDate.AddDays(1);
                }

                // Топ услуг по выручке
                var topServices = paidOrders
                    .GroupBy(o => o.service?.name ?? "Неизвестно")
                    .Select(g => new ServiceRevenue
                    {
                        ServiceName = g.Key,
                        Revenue = g.Sum(o => o.totalprice),
                        OrderCount = g.Count()
                    })
                    .Where(s => s.Revenue > 0)
                    .OrderByDescending(s => s.Revenue)
                    .Take(5)
                    .ToList();

                Console.WriteLine($"Найдено услуг в топе: {topServices.Count}");
                foreach (var service in topServices)
                {
                    Console.WriteLine($"Услуга: {service.ServiceName}, Выручка: {service.Revenue}, Заказов: {service.OrderCount}");
                }

                // Статистика по клиентам
                var customerStats = paidOrders
                    .GroupBy(o => o.userid)
                    .Select(g => new CustomerStats
                    {
                        UserId = g.Key,
                        UserName = g.First().user?.name ?? "Неизвестно",
                        OrderCount = g.Count(),
                        TotalSpent = g.Sum(o => o.totalprice)
                    })
                    .Where(c => c.TotalSpent > 0)
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(10)
                    .ToList();

                return new RevenueReport
                {
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    TotalOrders = allOrders.Count,
                    CompletedOrders = paidOrders.Count,
                    CancelledOrders = cancelledOrders.Count,
                    TotalRevenue = totalRevenue,
                    AverageOrderValue = averageOrderValue,
                    RevenueByDay = revenueByDay,
                    TopServices = topServices,
                    CustomerStatistics = customerStats
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading revenue report: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new RevenueReport();
            }
        }

        // 5. Экспорт отчета о выручке в PDF - ИСПРАВЛЕННЫЙ
        public async Task<byte[]> ExportRevenueReportToPdfAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                Console.WriteLine("Начало экспорта в PDF...");

                var report = await GetRevenueReportAsync(startDate, endDate);

                Console.WriteLine($"Получен отчет: Выручка = {report.TotalRevenue}, Услуг в топе = {report.TopServices?.Count ?? 0}");

                using var memoryStream = new MemoryStream();
                var document = new Document(PageSize.A4.Rotate(), 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, memoryStream);

                document.Open();

                // Заголовок
                var titleFont = FontFactory.GetFont("Arial", 18, Font.BOLD);
                var title = new Paragraph("ОТЧЕТ О ВЫРУЧКЕ", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(title);

                // Период
                var periodFont = FontFactory.GetFont("Arial", 12);
                var period = new Paragraph($"Период: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}", periodFont)
                {
                    SpacingAfter = 10
                };
                document.Add(period);

                // Основные показатели
                document.Add(new Paragraph("Основные показатели:", FontFactory.GetFont("Arial", 14, Font.BOLD))
                {
                    SpacingAfter = 10
                });

                var statsTable = new PdfPTable(4)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 10,
                    SpacingAfter = 20
                };

                statsTable.SetWidths(new float[] { 1, 1, 1, 1 });

                // Добавляем строки с метриками
                AddStatRow(statsTable, "Общая выручка:", $"{report.TotalRevenue:N0} ₽");
                AddStatRow(statsTable, "Средний чек:", $"{report.AverageOrderValue:N0} ₽");
                AddStatRow(statsTable, "Всего заказов:", report.TotalOrders.ToString());
                AddStatRow(statsTable, "Завершено:", report.CompletedOrders.ToString());
                AddStatRow(statsTable, "Отменено:", report.CancelledOrders.ToString());

                document.Add(statsTable);

                // График выручки по дням
                if (report.RevenueByDay != null && report.RevenueByDay.Any())
                {
                    document.Add(new Paragraph("Выручка по дням:", FontFactory.GetFont("Arial", 14, Font.BOLD))
                    {
                        SpacingBefore = 10,
                        SpacingAfter = 10
                    });

                    var chartTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20
                    };

                    AddTableHeader(chartTable, "Дата");
                    AddTableHeader(chartTable, "Выручка");

                    foreach (var item in report.RevenueByDay.OrderBy(x => x.Key))
                    {
                        chartTable.AddCell(CreateCell(item.Key.ToString("dd.MM.yyyy"), false));
                        chartTable.AddCell(CreateCell($"{item.Value:N0} ₽", false));
                    }

                    document.Add(chartTable);
                }

                // Топ услуг по выручке - ВЫВОДИМ ТОЛЬКО ЕСЛИ ЕСТЬ ДАННЫЕ
                if (report.TopServices != null && report.TopServices.Any())
                {
                    Console.WriteLine($"Добавляем {report.TopServices.Count} услуг в PDF");

                    document.Add(new Paragraph("Топ услуг по выручке:", FontFactory.GetFont("Arial", 14, Font.BOLD))
                    {
                        SpacingBefore = 10,
                        SpacingAfter = 10
                    });

                    var servicesTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20
                    };

                    servicesTable.SetWidths(new float[] { 2, 1, 1 });

                    AddTableHeader(servicesTable, "Услуга");
                    AddTableHeader(servicesTable, "Кол-во заказов");
                    AddTableHeader(servicesTable, "Выручка");

                    foreach (var service in report.TopServices)
                    {
                        servicesTable.AddCell(CreateCell(service.ServiceName, false));
                        servicesTable.AddCell(CreateCell(service.OrderCount.ToString(), false));
                        servicesTable.AddCell(CreateCell($"{service.Revenue:N0} ₽", false));
                    }

                    document.Add(servicesTable);
                }
                else
                {
                    Console.WriteLine("Нет данных для топ услуг");
                    document.Add(new Paragraph("Нет данных о топ услугах", FontFactory.GetFont("Arial", 12, Font.ITALIC))
                    {
                        SpacingBefore = 10,
                        SpacingAfter = 20
                    });
                }

                // Топ клиентов
                if (report.CustomerStatistics != null && report.CustomerStatistics.Any())
                {
                    document.Add(new Paragraph("Топ клиентов:", FontFactory.GetFont("Arial", 14, Font.BOLD))
                    {
                        SpacingBefore = 10,
                        SpacingAfter = 10
                    });

                    var customersTable = new PdfPTable(3)
                    {
                        WidthPercentage = 100,
                        SpacingAfter = 20
                    };

                    customersTable.SetWidths(new float[] { 2, 1, 1 });

                    AddTableHeader(customersTable, "Клиент");
                    AddTableHeader(customersTable, "Кол-во заказов");
                    AddTableHeader(customersTable, "Общая сумма");

                    foreach (var customer in report.CustomerStatistics)
                    {
                        customersTable.AddCell(CreateCell(customer.UserName, false));
                        customersTable.AddCell(CreateCell(customer.OrderCount.ToString(), false));
                        customersTable.AddCell(CreateCell($"{customer.TotalSpent:N0} ₽", false));
                    }

                    document.Add(customersTable);
                }

                // Дата генерации
                var dateFont = FontFactory.GetFont("Arial", 10, Font.ITALIC);
                var generationDate = new Paragraph(
                    $"Отчет сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm}", dateFont)
                {
                    Alignment = Element.ALIGN_RIGHT,
                    SpacingBefore = 30
                };
                document.Add(generationDate);

                document.Close();

                Console.WriteLine("PDF успешно создан");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting PDF: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        // Вспомогательные методы
        private void AddStatRow(PdfPTable table, string label, string value)
        {
            table.AddCell(CreateCell(label, true));
            table.AddCell(CreateCell(value, false));
            table.AddCell(CreateCell("", false)); // Пустые ячейки для выравнивания
            table.AddCell(CreateCell("", false));
        }

        private void AddTableHeader(PdfPTable table, string text)
        {
            table.AddCell(CreateCell(text, true));
        }

        private PdfPCell CreateCell(string text, bool isHeader)
        {
            var font = isHeader ?
                FontFactory.GetFont("Arial", 10, Font.BOLD) :
                FontFactory.GetFont("Arial", 10);

            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 8,
                BorderWidth = 0.5f
            };

            if (isHeader)
            {
                cell.BackgroundColor = new BaseColor(200, 200, 200);
            }

            return cell;
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
    }
}
