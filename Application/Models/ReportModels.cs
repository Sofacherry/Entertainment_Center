using System;
using System.Collections.Generic;

namespace BLL.Models
{
    public class PopularServiceReport
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public double? AveragePeople { get; set; } // Изменено на nullable
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

        // График выручки по дням
        public Dictionary<DateTime, decimal> RevenueByDay { get; set; } = new();

        public List<ServiceRevenue> TopServices { get; set; } = new();
        public List<CustomerStats> CustomerStatistics { get; set; } = new();
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

    public class DashboardMetrics
    {
        // Общая статистика
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TodayOrders { get; set; }
        public decimal TodayRevenue { get; set; }
        public int ActiveUsers { get; set; }
        public int VisitStatistics { get; set; }

        // Популярные услуги
        public string MostPopularService { get; set; }
        public int MostPopularServiceCount { get; set; }
        public decimal MostPopularServiceRevenue { get; set; }

        // Самый популярный месяц
        public string MostPopularMonth { get; set; }
        public int MostPopularMonthOrders { get; set; }

        // График выручки (последние 7 дней)
        public Dictionary<DateTime, decimal> Last7DaysRevenue { get; set; } = new();
    }

    public class RevenueByDay
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }
}