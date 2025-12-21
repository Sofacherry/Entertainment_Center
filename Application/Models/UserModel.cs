namespace BLL.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Passwordhash { get; set; }
        public string CitizenCategory { get; set; }

        public bool IsAdmin => Role == "admin";
        public bool IsBlocked => Role == "blocked";
    }
}

namespace BLL.Models
{
    public class OrderModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public int PeopleCount { get; set; }
        public string Status { get; set; }
        public List<ResourceModel> Resources { get; set; } = new();

        public string StatusColor => Status switch
        {
            "создан" => "#2196F3",
            "оплачен" => "#4CAF50",
            "подтвержден" => "#8BC34A",
            "отменен" => "#F44336",
            _ => "#9E9E9E"
        };
    }
}