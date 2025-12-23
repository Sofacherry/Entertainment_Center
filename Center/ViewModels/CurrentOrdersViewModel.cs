using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL;
using BLL.Models;
using Center.Views;  // Добавьте эту директиву для использования OrderDetailsWindow

namespace Center.ViewModels
{
    public class CurrentOrdersViewModel : INotifyPropertyChanged
    {
        private readonly BookingService _bookingService;
        private readonly int _currentUserId;

        private ObservableCollection<OrderViewModel> _allOrders = new();
        private ObservableCollection<OrderViewModel> _filteredOrders = new();

        public ObservableCollection<OrderViewModel> Orders
        {
            get => _filteredOrders;
            set => SetField(ref _filteredOrders, value);
        }

        // Флаги фильтров
        private bool _showAllOrders = true;
        public bool ShowAllOrders
        {
            get => _showAllOrders;
            set
            {
                if (SetField(ref _showAllOrders, value) && value)
                {
                    ApplyFilter("all");
                }
            }
        }

        private bool _showCurrentOrders;
        public bool ShowCurrentOrders
        {
            get => _showCurrentOrders;
            set
            {
                if (SetField(ref _showCurrentOrders, value) && value)
                {
                    ApplyFilter("current");
                }
            }
        }

        private bool _showCompletedOrders;
        public bool ShowCompletedOrders
        {
            get => _showCompletedOrders;
            set
            {
                if (SetField(ref _showCompletedOrders, value) && value)
                {
                    ApplyFilter("completed");
                }
            }
        }

        private bool _showCancelledOrders;
        public bool ShowCancelledOrders
        {
            get => _showCancelledOrders;
            set
            {
                if (SetField(ref _showCancelledOrders, value) && value)
                {
                    ApplyFilter("cancelled");
                }
            }
        }

        // Флаг наличия заказов
        public bool HasOrders => _allOrders.Any();

        // Команды
        public ICommand LoadOrdersCommand { get; }
        public ICommand ViewDetailsCommand { get; }
        public ICommand CancelOrderCommand { get; }
        public ICommand SetReminderCommand { get; }
        public ICommand RefreshOrdersCommand { get; }
        public ICommand DeleteOrderCommand { get; }  // Новая команда для удаления

        public CurrentOrdersViewModel(BookingService bookingService, int currentUserId)
        {
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _currentUserId = currentUserId;

            // Используем встроенные классы Command
            LoadOrdersCommand = new Command(async () => await LoadOrdersAsync());
            ViewDetailsCommand = new Command<OrderViewModel>(ViewOrderDetails);
            CancelOrderCommand = new Command<OrderViewModel>(async order => await CancelOrderAsync(order));
            SetReminderCommand = new Command<OrderViewModel>(SetReminder);
            RefreshOrdersCommand = new Command(async () => await RefreshOrdersAsync());
            DeleteOrderCommand = new Command<OrderViewModel>(async order => await DeleteOrderAsync(order));  // Новая команда

            // Загружаем заказы при создании
            _ = LoadOrdersAsync();

            // Запускаем таймер для проверки завершения заказов
            StartOrderCompletionTimer();
        }

        private async Task LoadOrdersAsync()
        {
            try
            {
                // Получаем заказы через сервис
                var orders = await _bookingService.GetUserOrdersAsync(_currentUserId);

                // Конвертируем в OrderModel
                var orderModels = MapToOrderModels(orders);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allOrders.Clear();
                    foreach (var orderModel in orderModels)
                    {
                        _allOrders.Add(new OrderViewModel(orderModel));
                    }

                    ApplyFilter("all");
                    OnPropertyChanged(nameof(HasOrders));
                });

                // Проверяем и завершаем прошедшие заказы
                await CheckAndCompletePastOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<OrderModel> MapToOrderModels(List<DAL.Entities.Orders> orders)
        {
            var result = new List<OrderModel>();

            foreach (var order in orders)
            {
                var orderModel = new OrderModel
                {
                    Id = order.orderid,
                    UserId = order.userid,
                    UserName = order.user?.name ?? "Пользователь",
                    ServiceId = order.serviceid,
                    ServiceName = order.service?.name ?? "Услуга",
                    OrderDate = order.orderdate,
                    TotalPrice = order.totalprice,
                    PeopleCount = order.peoplecount,
                    Status = order.status
                };

                // Добавляем ресурсы
                if (order.orderresources != null)
                {
                    foreach (var orderResource in order.orderresources)
                    {
                        if (orderResource.resource != null)
                        {
                            orderModel.Resources.Add(new ResourceModel
                            {
                                Id = orderResource.resource.resourceid,
                                Name = orderResource.resource.name,
                                Capacity = orderResource.resource.capacity
                            });
                        }
                    }
                }

                result.Add(orderModel);
            }

            return result;
        }

        private void ApplyFilter(string filterType)
        {
            if (!_allOrders.Any())
            {
                Orders = new ObservableCollection<OrderViewModel>();
                return;
            }

            var now = DateTime.Now;
            var filtered = filterType switch
            {
                "current" => _allOrders.Where(o => o.IsActive).ToList(),
                "completed" => _allOrders.Where(o => o.Status == "завершен").ToList(),
                "cancelled" => _allOrders.Where(o => o.Status == "отменен").ToList(),
                _ => _allOrders.ToList() // all
            };

            // Сортируем: сначала текущие (по дате), потом завершенные/отмененные (по дате)
            var sorted = filtered
                .OrderByDescending(o => o.IsActive)
                .ThenByDescending(o => o.OrderDate)
                .ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                Orders.Clear();
                foreach (var order in sorted)
                {
                    Orders.Add(order);
                }
            });
        }

        private void ViewOrderDetails(OrderViewModel order)
        {
            if (order == null) return;

            // Вместо MessageBox используем новое красивое окно
            var detailsWindow = new OrderDetailsWindow(order);
            detailsWindow.Owner = Application.Current.MainWindow;
            detailsWindow.ShowDialog();
        }

        private async Task CancelOrderAsync(OrderViewModel order)
        {
            if (order == null) return;

            if (MessageBox.Show(
                $"Вы уверены, что хотите отменить заказ #{order.Id}?\n" +
                $"Услуга: {order.ServiceName}\n" +
                $"Дата: {order.OrderDate:dd.MM.yyyy HH:mm}",
                "Отмена заказа",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var success = await _bookingService.CancelOrderAsync(order.Id, _currentUserId);

                if (success)
                {
                    MessageBox.Show("Заказ успешно отменен", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Обновляем список заказов
                    await RefreshOrdersAsync();
                }
                else
                {
                    MessageBox.Show("Не удалось отменить заказ", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отмене заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteOrderAsync(OrderViewModel order)
        {
            if (order == null) return;

            if (MessageBox.Show(
                $"Вы уверены, что хотите удалить заказ #{order.Id} из списка?\n" +
                $"Услуга: {order.ServiceName}\n" +
                $"Дата: {order.OrderDate:dd.MM.yyyy HH:mm}\n\n" +
                $"Заказ будет удален только из вашего списка, но останется в системе.",
                "Удаление заказа",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                // Удаляем из локальной коллекции (мягкое удаление - только из вида пользователя)
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allOrders.Remove(order);
                    ApplyFilter("all");
                    OnPropertyChanged(nameof(HasOrders));
                });

                MessageBox.Show("Заказ удален из списка", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении заказа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetReminder(OrderViewModel order)
        {
            if (order == null) return;

            // TODO: Реализовать логику напоминания
            MessageBox.Show(
                $"Напоминание для заказа #{order.Id} установлено!\n" +
                $"Время начала: {order.OrderDate:dd.MM.yyyy HH:mm}",
                "Напоминание",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async Task RefreshOrdersAsync()
        {
            await LoadOrdersAsync();
        }

        private async Task CheckAndCompletePastOrders()
        {
            try
            {
                var now = DateTime.Now;
                var ordersToComplete = _allOrders
                    .Where(o => o.ShouldBeCompleted())
                    .ToList();

                foreach (var order in ordersToComplete)
                {
                    var success = await _bookingService.UpdateOrderStatusAsync(order.Id, "завершен");
                    if (success)
                    {
                        // Обновляем статус в локальной коллекции
                        order.GetType().GetProperty("Status")?.SetValue(order, "завершен");
                    }
                }

                if (ordersToComplete.Any())
                {
                    await RefreshOrdersAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при завершении заказов: {ex.Message}");
            }
        }

        private void StartOrderCompletionTimer()
        {
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(5); // Проверяем каждые 5 минут
            timer.Tick += async (s, e) => await CheckAndCompletePastOrders();
            timer.Start();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Встроенные классы Command для этого ViewModel
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

        private class Command<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Func<T, bool> _canExecute;

            public Command(Action<T> execute, Func<T, bool> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                if (parameter == null && typeof(T).IsValueType)
                    return false;
                return _canExecute?.Invoke((T)parameter) ?? true;
            }

            public void Execute(object parameter)
            {
                if (parameter is T typedParameter)
                {
                    _execute(typedParameter);
                }
                else if (parameter == null && !typeof(T).IsValueType)
                {
                    _execute(default);
                }
            }

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }
        }
    }
}