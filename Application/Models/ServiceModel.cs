using System;
using System.Collections.Generic;

namespace BLL.Models
{
    public class ServiceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; } = "";
        public int Duration { get; set; }
        public decimal WeekdayPrice { get; set; }
        public decimal WeekendPrice { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public string DurationDisplay => $"{Duration} мин.";
        public string WeekdayPriceDisplay => $"{WeekdayPrice:N2} руб.";
        public string WeekendPriceDisplay => $"{WeekendPrice:N2} руб.";
        public string TimeRangeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";

        public List<ResourceModel> Resources { get; set; } = new();
        public List<CategoryModel> Categories { get; set; } = new();
    }

    public class CategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ResourceModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
    }
}