namespace BLL.Models
{
    // Модели для административной панели
    public class ServiceAdminModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Duration { get; set; }
        public decimal WeekdayPrice { get; set; }
        public decimal WeekendPrice { get; set; }
        public System.TimeSpan StartTime { get; set; }
        public System.TimeSpan EndTime { get; set; }
    }

    public class ResourceAdminModel
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
    }

    public class CategoryAdminModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CitizenCategoryAdminModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Discount { get; set; }
    }

    public class UserAdminModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int CitizenCategoryId { get; set; }
        public string CitizenCategoryName { get; set; }
    }

    public class OrderAdminModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public System.DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public int PeopleCount { get; set; }
        public string Status { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
