using Microsoft.EntityFrameworkCore;
using DAL;
using DAL.Entities;
using BLL.Models;

namespace BLL
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // 1. Регистрация нового пользователя
        public async Task<UserModel?> RegisterUserAsync(string name, string email, string password,
            int citizenCategoryId, string role = "client")
        {
            var categoryExists = await _context.citizencategories
                .AnyAsync(c => c.categoryid == citizenCategoryId);

            if (!categoryExists)
                return null;

            var existingUser = await _context.users
                .FirstOrDefaultAsync(u => u.email == email);

            if (existingUser != null)
                return null;

            var user = new Users
            {
                name = name,
                email = email,
                passwordhash = password,
                citizencategoryid = citizenCategoryId,
                role = role
            };

            _context.users.Add(user);
            await _context.SaveChangesAsync();

            return ToUserModel(user);
        }

        // 2. Авторизация
        public async Task<UserModel?> AuthenticateAsync(string email, string password)
        {
            var user = await _context.users
                .Include(u => u.citizencategory)
                .FirstOrDefaultAsync(u => u.email == email);

            if (user == null)
                return null;

            return ToUserModel(user);
        }

        // 3. Получение профиля
        public async Task<UserModel?> GetUserProfileAsync(int userId)
        {
            var entity = await _context.users
                .Include(u => u.citizencategory)
                .FirstOrDefaultAsync(u => u.userid == userId);

            return ToUserModel(entity);
        }

        // 4. Обновление профиля
        public async Task<bool> UpdateUserProfileAsync(int userId, UserUpdateData updateData)
        {
            var user = await _context.users.FindAsync(userId);
            if (user == null)
                return false;

            user.name = updateData.Name ?? user.name;
            user.citizencategoryid = updateData.CitizenCategoryId;

            if (!string.IsNullOrEmpty(updateData.NewPassword))
                user.passwordhash = updateData.NewPassword;

            await _context.SaveChangesAsync();
            return true;
        }

        // 5. Получить скидку пользователя
        public async Task<decimal> GetUserDiscountAsync(int userId)
        {
            var user = await _context.users
                .Include(u => u.citizencategory)
                .FirstOrDefaultAsync(u => u.userid == userId);

            return user?.citizencategory?.discount ?? 0;
        }

        // 6. Применить скидку к цене
        public decimal ApplyDiscount(decimal price, decimal discount)
        {
            if (discount <= 0 || discount >= 1)
                return price;

            return price * (1 - discount);
        }

        // 7. Получить все категории граждан
        public async Task<List<CitizenCategoryModel>> GetAllCitizenCategoriesAsync()
        {
            var categories = await _context.citizencategories
                .OrderBy(c => c.categoryname)
                .ToListAsync();

            return categories.Select(c => new CitizenCategoryModel
            {
                Id = c.categoryid,
                Name = c.categoryname,
                Discount = c.discount
            }).ToList();
        }

        // 8. Изменить категорию граждан пользователя
        public async Task<bool> ChangeUserCitizenCategoryAsync(int userId, int categoryId)
        {
            var user = await _context.users.FindAsync(userId);
            if (user == null)
                return false;

            user.citizencategoryid = categoryId;
            await _context.SaveChangesAsync();
            return true;
        }

        // 9. Проверить, занят ли email
        public async Task<bool> IsEmailTakenAsync(string email, int? excludeUserId = null)
        {
            var query = _context.users.Where(u => u.email == email);

            if (excludeUserId.HasValue)
                query = query.Where(u => u.userid != excludeUserId.Value);

            return await query.AnyAsync();
        }

        // 10. Смена пароля
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.users.FindAsync(userId);
            if (user == null)
                return false;

            user.passwordhash = newPassword;
            await _context.SaveChangesAsync();
            return true;
        }

        // 11. Получить пользователя по email (для восстановления пароля)
        public async Task<UserModel?> GetUserByEmailAsync(string email)
        {
            var entity = await _context.users
                .Include(u => u.citizencategory)
                .FirstOrDefaultAsync(u => u.email == email);

            return ToUserModel(entity);
        }

        // 12. Получить список всех пользователей
        public async Task<List<UserModel>> GetAllUsersAsync()
        {
            var users = await _context.users
                .Include(u => u.citizencategory)
                .OrderBy(u => u.name)
                .ToListAsync();

            return users.Select(ToUserModel).ToList();
        }

        // Метод преобразования Users → UserModel
        private UserModel ToUserModel(Users entity)
        {
            if (entity == null) return null;

            return new UserModel
            {
                Id = entity.userid,
                Name = entity.name,
                Email = entity.email,
                Role = entity.role,
                Passwordhash = entity.passwordhash,
                CitizenCategory = entity.citizencategory?.categoryname
            };
        }

        public class UserUpdateData
        {
            public string? Name { get; set; }
            public int CitizenCategoryId { get; set; }
            public string? NewPassword { get; set; }
        }

        public class CitizenCategoryModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Discount { get; set; }
        }
    }
}
