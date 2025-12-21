using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Entities;
using System.Resources;
using System;

namespace BLL
{
    public class AdminCRUDService
    {
        private readonly AppDbContext _context;

        public AdminCRUDService(AppDbContext context)
        {
            _context = context;
        }

        // Services

        public async Task<Services> CreateServiceAsync(ServiceData data)
        {
            var service = new Services
            {
                name = data.Name,
                duration = data.Duration,
                weekdayprice = data.WeekdayPrice,
                weekendprice = data.WeekendPrice,
                starttime = data.StartTime,
                endtime = data.EndTime
            };

            _context.services.Add(service);
            await _context.SaveChangesAsync();
            return service;
        }

        public async Task UpdateServiceAsync(int serviceId, ServiceData data)
        {
            var service = await _context.services.FindAsync(serviceId);
            if (service == null)
                throw new Exception($"Услуга с ID {serviceId} не найдена");

            service.name = data.Name;
            service.duration = data.Duration;
            service.weekdayprice = data.WeekdayPrice;
            service.weekendprice = data.WeekendPrice;
            service.starttime = data.StartTime;
            service.endtime = data.EndTime;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteServiceAsync(int serviceId)
        {
            var service = await _context.services.FindAsync(serviceId);
            if (service != null)
            {
                _context.services.Remove(service);
                await _context.SaveChangesAsync();
            }
        }

        // Resources

        public async Task<Resources> AddResourceToServiceAsync(int serviceId, ResourceData data)
        {
            var resource = new Resources
            {
                serviceid = serviceId,
                name = data.Name,
                capacity = data.Capacity
            };

            _context.resources.Add(resource);
            await _context.SaveChangesAsync();
            return resource;
        }

        public async Task UpdateResourceAsync(int resourceId, ResourceData data)
        {
            var resource = await _context.resources.FindAsync(resourceId);
            if (resource == null)
                throw new Exception($"Ресурс с ID {resourceId} не найден");

            resource.name = data.Name;
            resource.capacity = data.Capacity;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteResourceAsync(int resourceId)
        {
            var resource = await _context.resources.FindAsync(resourceId);
            if (resource != null)
            {
                _context.resources.Remove(resource);
                await _context.SaveChangesAsync();
            }
        }

        // Categories

        public async Task<Categories> CreateCategoryAsync(string name)
        {
            var category = new Categories
            {
                categoryname = name
            };

            _context.categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateCategoryAsync(int categoryId, string newName)
        {
            var category = await _context.categories.FindAsync(categoryId);
            if (category == null)
                throw new Exception($"Категория с ID {categoryId} не найдена");

            category.categoryname = newName;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            var category = await _context.categories.FindAsync(categoryId);
            if (category != null)
            {
                _context.categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

        // Citizencategories

        public async Task<Citizencategories> CreateCitizenCategoryAsync(string name, decimal discount)
        {
            var category = new Citizencategories
            {
                categoryname = name,
                discount = discount
            };

            _context.citizencategories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task UpdateCitizenCategoryAsync(int categoryId, CitizenCategoryData data)
        {
            var category = await _context.citizencategories.FindAsync(categoryId);
            if (category == null)
                throw new Exception($"Категория граждан с ID {categoryId} не найдена");

            category.categoryname = data.Name;
            category.discount = data.Discount;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteCitizenCategoryAsync(int categoryId)
        {
            var category = await _context.citizencategories.FindAsync(categoryId);
            if (category != null)
            {
                _context.citizencategories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

        // Users

        public async Task<List<Users>> GetAllUsersAsync()
        {
            return await _context.users
                .Include(u => u.citizencategory)
                .OrderBy(u => u.name)
                .ToListAsync();
        }

        public async Task BlockUserAsync(int userId)
        {
            var user = await _context.users.FindAsync(userId);
            if (user != null)
            {
                user.role = "blocked";
                await _context.SaveChangesAsync();
            }
        }

        public async Task UnblockUserAsync(int userId)
        {
            var user = await _context.users.FindAsync(userId);
            if (user != null && user.role == "blocked")
            {
                user.role = "client";
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _context.users.FindAsync(userId);
            if (user != null)
            {
                _context.users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Users> CreateUserAsync(UserData data)
        {
            var user = new Users
            {
                name = data.Name,
                email = data.Email,
                passwordhash = data.Password,
                role = data.Role, // "client" или "admin"
                citizencategoryid = data.CitizenCategoryId
            };

            _context.users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task UpdateUserAsync(int userId, UserData data)
        {
            var user = await _context.users.FindAsync(userId);
            if (user == null)
                throw new Exception($"Пользователь с ID {userId} не найден");

            user.name = data.Name;
            user.email = data.Email;
            user.role = data.Role;
            user.citizencategoryid = data.CitizenCategoryId;

            // Если нужно обновить пароль
            if (!string.IsNullOrEmpty(data.Password))
                user.passwordhash = data.Password;

            await _context.SaveChangesAsync();
        }

        // Orders

        public async Task<Orders> CreateOrderAsync(OrderData data)
        {
            var order = new Orders
            {
                userid = data.UserId,
                serviceid = data.ServiceId,
                orderdate = data.OrderDate,
                totalprice = data.TotalPrice,
                peoplecount = data.PeopleCount,
                status = data.Status ?? "создан"
            };

            _context.orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateOrderAsync(int orderId, OrderData data)
        {
            var order = await _context.orders.FindAsync(orderId);
            if (order == null)
                throw new Exception($"Заказ с ID {orderId} не найден");

            order.userid = data.UserId;
            order.serviceid = data.ServiceId;
            order.orderdate = data.OrderDate;
            order.totalprice = data.TotalPrice;
            order.peoplecount = data.PeopleCount;
            order.status = data.Status ?? order.status;

            await _context.SaveChangesAsync();
        }

        public async Task<List<Orders>> GetAllOrdersAsync()
        {
            return await _context.orders
                .Include(o => o.user)
                .Include(o => o.service)
                .OrderByDescending(o => o.orderdate)
                .ToListAsync();
        }

        public async Task ForceCancelOrderAsync(int orderId)
        {
            var order = await _context.orders.FindAsync(orderId);
            if (order != null)
            {
                order.status = "отменен (администратором)";
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteOrderAsync(int orderId)
        {
            var order = await _context.orders.FindAsync(orderId);
            if (order != null)
            {
                _context.orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        // Servicecategories

        public async Task AddServiceToCategoryAsync(int serviceId, int categoryId)
        {
            var existingLink = await _context.servicecategories
                .FirstOrDefaultAsync(sc => sc.serviceid == serviceId && sc.categoryid == categoryId);

            if (existingLink != null)
                throw new Exception("Эта услуга уже в данной категории");

            var link = new Servicecategories
            {
                serviceid = serviceId,
                categoryid = categoryId
            };

            _context.servicecategories.Add(link);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveServiceFromCategoryAsync(int serviceId, int categoryId)
        {
            var link = await _context.servicecategories
                .FirstOrDefaultAsync(sc => sc.serviceid == serviceId && sc.categoryid == categoryId);

            if (link != null)
            {
                _context.servicecategories.Remove(link);
                await _context.SaveChangesAsync();
            }
        }
    }

    public class ServiceData
    {
        public string Name { get; set; }
        public int Duration { get; set; }
        public decimal WeekdayPrice { get; set; }
        public decimal WeekendPrice { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class ResourceData
    {
        public string Name { get; set; }
        public int Capacity { get; set; }
    }

    public class CitizenCategoryData
    {
        public string Name { get; set; }
        public decimal Discount { get; set; }
    }

    public class UserData
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "client";
        public int CitizenCategoryId { get; set; }
    }

    public class OrderData
    {
        public int UserId { get; set; }
        public int ServiceId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public int PeopleCount { get; set; }
        public string Status { get; set; }
    }
}