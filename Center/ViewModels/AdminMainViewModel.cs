using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL;
using BLL.Models;

namespace Center.ViewModels
{
    /// <summary>
    /// ViewModel для административной панели с CRUD операциями для всех сущностей
    /// </summary>
    public class AdminMainViewModel : INotifyPropertyChanged
    {
        private readonly AdminCRUDService _adminCRUDService;
        private readonly BookingService _bookingService;
        private readonly CatalogService _catalogService;
        private readonly UserService _userService;

        // Коллекции для отображения
        public ObservableCollection<ServiceAdminModel> Services { get; }
        public ObservableCollection<ResourceAdminModel> Resources { get; }
        public ObservableCollection<CategoryAdminModel> Categories { get; }
        public ObservableCollection<CitizenCategoryAdminModel> CitizenCategories { get; }
        public ObservableCollection<UserAdminModel> Users { get; }
        public ObservableCollection<OrderAdminModel> Orders { get; }

        // Выбранные элементы
        private ServiceAdminModel _selectedService;
        public ServiceAdminModel SelectedService
        {
            get => _selectedService;
            set => SetField(ref _selectedService, value);
        }

        private ResourceAdminModel _selectedResource;
        public ResourceAdminModel SelectedResource
        {
            get => _selectedResource;
            set => SetField(ref _selectedResource, value);
        }

        private CategoryAdminModel _selectedCategory;
        public CategoryAdminModel SelectedCategory
        {
            get => _selectedCategory;
            set => SetField(ref _selectedCategory, value);
        }

        private CitizenCategoryAdminModel _selectedCitizenCategory;
        public CitizenCategoryAdminModel SelectedCitizenCategory
        {
            get => _selectedCitizenCategory;
            set => SetField(ref _selectedCitizenCategory, value);
        }

        private UserAdminModel _selectedUser;
        public UserAdminModel SelectedUser
        {
            get => _selectedUser;
            set => SetField(ref _selectedUser, value);
        }

        private OrderAdminModel _selectedOrder;
        public OrderAdminModel SelectedOrder
        {
            get => _selectedOrder;
            set => SetField(ref _selectedOrder, value);
        }

        // Команды для Services
        public ICommand CreateServiceCommand { get; }
        public ICommand UpdateServiceCommand { get; }
        public ICommand DeleteServiceCommand { get; }

        // Команды для Resources
        public ICommand CreateResourceCommand { get; }
        public ICommand UpdateResourceCommand { get; }
        public ICommand DeleteResourceCommand { get; }

        // Команды для Categories
        public ICommand CreateCategoryCommand { get; }
        public ICommand UpdateCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }

        // Команды для CitizenCategories
        public ICommand CreateCitizenCategoryCommand { get; }
        public ICommand UpdateCitizenCategoryCommand { get; }
        public ICommand DeleteCitizenCategoryCommand { get; }

        // Команды для Users
        public ICommand CreateUserCommand { get; }
        public ICommand UpdateUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand BlockUserCommand { get; }
        public ICommand UnblockUserCommand { get; }

        // Команды для Orders
        public ICommand UpdateOrderStatusCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand DeleteOrderCommand { get; }

        // Команда обновления данных
        public ICommand RefreshDataCommand { get; }

        public AdminMainViewModel(
            AdminCRUDService adminCRUDService,
            BookingService bookingService,
            CatalogService catalogService,
            UserService userService)
        {
            _adminCRUDService = adminCRUDService ?? throw new ArgumentNullException(nameof(adminCRUDService));
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

            Services = new ObservableCollection<ServiceAdminModel>();
            Resources = new ObservableCollection<ResourceAdminModel>();
            Categories = new ObservableCollection<CategoryAdminModel>();
            CitizenCategories = new ObservableCollection<CitizenCategoryAdminModel>();
            Users = new ObservableCollection<UserAdminModel>();
            Orders = new ObservableCollection<OrderAdminModel>();

            // Инициализация команд - используем встроенные классы Command
            CreateServiceCommand = new Command(CreateService);
            UpdateServiceCommand = new Command(UpdateService, () => SelectedService != null);
            DeleteServiceCommand = new Command(DeleteService, () => SelectedService != null);

            CreateResourceCommand = new Command(CreateResource);
            UpdateResourceCommand = new Command(UpdateResource, () => SelectedResource != null);
            DeleteResourceCommand = new Command(DeleteResource, () => SelectedResource != null);

            CreateCategoryCommand = new Command(CreateCategory);
            UpdateCategoryCommand = new Command(UpdateCategory, () => SelectedCategory != null);
            DeleteCategoryCommand = new Command(DeleteCategory, () => SelectedCategory != null);

            CreateCitizenCategoryCommand = new Command(CreateCitizenCategory);
            UpdateCitizenCategoryCommand = new Command(UpdateCitizenCategory, () => SelectedCitizenCategory != null);
            DeleteCitizenCategoryCommand = new Command(DeleteCitizenCategory, () => SelectedCitizenCategory != null);

            CreateUserCommand = new Command(CreateUser);
            UpdateUserCommand = new Command(UpdateUser, () => SelectedUser != null);
            DeleteUserCommand = new Command(DeleteUser, () => SelectedUser != null);
            BlockUserCommand = new Command(BlockUser, () => SelectedUser != null);
            UnblockUserCommand = new Command(UnblockUser, () => SelectedUser != null);

            UpdateOrderStatusCommand = new Command(UpdateOrderStatus, () => SelectedOrder != null);
            CancelOrderCommand = new Command(CancelOrder, () => SelectedOrder != null);
            DeleteOrderCommand = new Command(DeleteOrder, () => SelectedOrder != null);

            RefreshDataCommand = new Command(async () => await LoadAllDataAsync());

            // Загружаем данные при инициализации
            _ = LoadAllDataAsync();
        }

        private async Task LoadAllDataAsync()
        {
            try
            {
                await Task.WhenAll(
                    LoadServicesAsync(),
                    LoadResourcesAsync(),
                    LoadCategoriesAsync(),
                    LoadCitizenCategoriesAsync(),
                    LoadUsersAsync(),
                    LoadOrdersAsync()
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadServicesAsync()
        {
            try
            {
                var services = await _catalogService.GetAllServicesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Services.Clear();
                    foreach (var service in services)
                    {
                        Services.Add(new ServiceAdminModel
                        {
                            Id = service.Id,
                            Name = service.Name,
                            Duration = service.Duration,
                            WeekdayPrice = service.WeekdayPrice,
                            WeekendPrice = service.WeekendPrice,
                            StartTime = service.StartTime,
                            EndTime = service.EndTime
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadResourcesAsync()
        {
            // Реализация загрузки ресурсов будет добавлена позже
            await Task.CompletedTask;
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _catalogService.GetAllCategoriesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(new CategoryAdminModel
                        {
                            Id = category.Id,
                            Name = category.Name
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCitizenCategoriesAsync()
        {
            try
            {
                var categories = await _userService.GetAllCitizenCategoriesAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CitizenCategories.Clear();
                    foreach (var category in categories)
                    {
                        CitizenCategories.Add(new CitizenCategoryAdminModel
                        {
                            Id = category.Id,
                            Name = category.Name,
                            Discount = category.Discount
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий граждан: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _adminCRUDService.GetAllUsersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var user in users)
                    {
                        Users.Add(new UserAdminModel
                        {
                            Id = user.userid,
                            Name = user.name,
                            Email = user.email,
                            Role = user.role,
                            CitizenCategoryId = user.citizencategoryid,
                            CitizenCategoryName = user.citizencategory?.categoryname ?? "Не указана"
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                var orders = await _adminCRUDService.GetAllOrdersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    foreach (var order in orders)
                    {
                        Orders.Add(new OrderAdminModel
                        {
                            Id = order.orderid,
                            UserId = order.userid,
                            UserName = order.user?.name ?? "Неизвестно",
                            ServiceId = order.serviceid,
                            ServiceName = order.service?.name ?? "Неизвестно",
                            OrderDate = order.orderdate,
                            TotalPrice = order.totalprice,
                            PeopleCount = order.peoplecount,
                            Status = order.status,
                            CreatedAt = order.created_at
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Методы для работы с Services
        private async void CreateService()
        {
            // TODO: Открыть диалог создания услуги
            MessageBox.Show("Функция создания услуги будет реализована", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void UpdateService()
        {
            // TODO: Открыть диалог редактирования услуги
            MessageBox.Show("Функция редактирования услуги будет реализована", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void DeleteService()
        {
            if (SelectedService == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить услугу '{SelectedService.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.DeleteServiceAsync(SelectedService.Id);
                    MessageBox.Show("Услуга успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadServicesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления услуги: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Методы для работы с Resources
        private async void CreateResource()
        {
            // TODO: Реализовать
        }

        private async void UpdateResource()
        {
            // TODO: Реализовать
        }

        private async void DeleteResource()
        {
            if (SelectedResource == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить ресурс '{SelectedResource.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.DeleteResourceAsync(SelectedResource.Id);
                    MessageBox.Show("Ресурс успешно удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadResourcesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления ресурса: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Методы для работы с Categories
        private async void CreateCategory()
        {
            // TODO: Реализовать
        }

        private async void UpdateCategory()
        {
            // TODO: Реализовать
        }

        private async void DeleteCategory()
        {
            if (SelectedCategory == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить категорию '{SelectedCategory.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.DeleteCategoryAsync(SelectedCategory.Id);
                    MessageBox.Show("Категория успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCategoriesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления категории: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Методы для работы с CitizenCategories
        private async void CreateCitizenCategory()
        {
            // TODO: Реализовать
        }

        private async void UpdateCitizenCategory()
        {
            // TODO: Реализовать
        }

        private async void DeleteCitizenCategory()
        {
            if (SelectedCitizenCategory == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить категорию граждан '{SelectedCitizenCategory.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.DeleteCitizenCategoryAsync(SelectedCitizenCategory.Id);
                    MessageBox.Show("Категория граждан успешно удалена", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCitizenCategoriesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления категории граждан: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Методы для работы с Users
        private async void CreateUser()
        {
            // TODO: Реализовать
        }

        private async void UpdateUser()
        {
            // TODO: Реализовать
        }

        private async void DeleteUser()
        {
            if (SelectedUser == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить пользователя '{SelectedUser.Name}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.DeleteUserAsync(SelectedUser.Id);
                    MessageBox.Show("Пользователь успешно удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadUsersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления пользователя: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BlockUser()
        {
            if (SelectedUser == null) return;

            try
            {
                await _adminCRUDService.BlockUserAsync(SelectedUser.Id);
                MessageBox.Show("Пользователь заблокирован", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка блокировки пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UnblockUser()
        {
            if (SelectedUser == null) return;

            try
            {
                await _adminCRUDService.UnblockUserAsync(SelectedUser.Id);
                MessageBox.Show("Пользователь разблокирован", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка разблокировки пользователя: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Методы для работы с Orders
        private async void UpdateOrderStatus()
        {
            if (SelectedOrder == null) return;

            // TODO: Открыть диалог выбора статуса
            MessageBox.Show("Функция изменения статуса заказа будет реализована", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void CancelOrder()
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите отменить заказ #{SelectedOrder.Id}?",
                "Подтверждение отмены",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.ForceCancelOrderAsync(SelectedOrder.Id);
                    MessageBox.Show("Заказ успешно отменен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка отмены заказа: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteOrder()
        {
            if (SelectedOrder == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить заказ #{SelectedOrder.Id}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _adminCRUDService.DeleteOrderAsync(SelectedOrder.Id);
                    MessageBox.Show("Заказ успешно удален", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadOrdersAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления заказа: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Встроенные классы Command (как в CatalogPageViewModel)
        private class Command : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public Command(Action execute, Func<bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
            public void Execute(object parameter) => _execute();
            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}