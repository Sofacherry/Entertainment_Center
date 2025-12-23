using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BLL;
using BLL.Models;

namespace Center.ViewModels
{
    public class BookingViewModel : INotifyPropertyChanged
    {
        private readonly BookingService _bookingService;
        private readonly UserService _userService;
        private readonly CatalogService _catalogService;
        private readonly int _currentUserId;
        private readonly int _serviceId;
        private CancellationTokenSource _discountCalculationCancellation;
        private bool _isLoadingResources = false;

        // Основная модель услуги
        private ServiceModel _service;
        public ServiceModel Service
        {
            get => _service;
            set => SetField(ref _service, value);
        }

        // Дополнительные свойства для отображения
        private string _serviceIcon;
        public string ServiceIcon
        {
            get => _serviceIcon;
            set => SetField(ref _serviceIcon, value);
        }

        private string _serviceCategory;
        public string ServiceCategory
        {
            get => _serviceCategory;
            set => SetField(ref _serviceCategory, value);
        }

        private int _maxCapacity;
        public int MaxCapacity
        {
            get => _maxCapacity;
            set => SetField(ref _maxCapacity, value);
        }

        private decimal _currentPrice;
        public decimal CurrentPrice
        {
            get => _currentPrice;
            set => SetField(ref _currentPrice, value);
        }

        // Вход пользователя
        private int _peopleCount;
        public int PeopleCount
        {
            get => _peopleCount;
            set
            {
                if (SetField(ref _peopleCount, value))
                {
                    if (value > 0)
                    {
                        _ = UpdateAvailableResourcesAsync();
                        CalculatePrices();
                    }
                }
            }
        }

        private DateTime? _selectedDate;
        public DateTime? SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetField(ref _selectedDate, value))
                {
                    UpdateTimeSlots();
                    _ = UpdateAvailableResourcesAsync();
                    CalculatePrices();
                }
            }
        }

        private DateTime? _selectedTimeSlot;
        public DateTime? SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set
            {
                if (SetField(ref _selectedTimeSlot, value))
                {
                    _ = UpdateAvailableResourcesAsync();
                    CalculatePrices();
                }
            }
        }

        private bool _includeInstructor;
        public bool IncludeInstructor
        {
            get => _includeInstructor;
            set
            {
                if (SetField(ref _includeInstructor, value))
                    CalculatePrices();
            }
        }

        private bool _includeEquipment;
        public bool IncludeEquipment
        {
            get => _includeEquipment;
            set
            {
                if (SetField(ref _includeEquipment, value))
                    CalculatePrices();
            }
        }

        private bool _includeFood;
        public bool IncludeFood
        {
            get => _includeFood;
            set
            {
                if (SetField(ref _includeFood, value))
                    CalculatePrices();
            }
        }

        // Цены
        private decimal _servicePrice;
        public decimal ServicePrice
        {
            get => _servicePrice;
            set => SetField(ref _servicePrice, value);
        }

        private decimal _extrasPrice;
        public decimal ExtrasPrice
        {
            get => _extrasPrice;
            set => SetField(ref _extrasPrice, value);
        }

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => SetField(ref _discountAmount, value);
        }

        private decimal _userDiscount;
        public decimal UserDiscount
        {
            get => _userDiscount;
            set => SetField(ref _userDiscount, value);
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => SetField(ref _totalPrice, value);
        }

        // Коллекции для отображения
        public ObservableCollection<DateTime> TimeSlots { get; }
        public ObservableCollection<BookingResourceViewModel> AvailableResources { get; }
        public List<int> SelectedResourceIds { get; }

        // Информация о необходимом количестве ресурсов
        private string _resourcesDescription;
        public string ResourcesDescription
        {
            get => _resourcesDescription;
            set => SetField(ref _resourcesDescription, value);
        }

        private int _requiredResourceCount;
        public int RequiredResourceCount
        {
            get => _requiredResourceCount;
            set => SetField(ref _requiredResourceCount, value);
        }

        // Команды
        public ICommand GoBackCommand { get; }
        public ICommand ProceedToPaymentCommand { get; }
        public ICommand SelectResourceCommand { get; }

        public BookingViewModel(
            BookingService bookingService,
            UserService userService,
            CatalogService catalogService,
            int serviceId,
            int currentUserId)
        {
            // Получаем сервисы через dependency injection
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));

            _serviceId = serviceId;
            _currentUserId = currentUserId;

            TimeSlots = new ObservableCollection<DateTime>();
            AvailableResources = new ObservableCollection<BookingResourceViewModel>();
            SelectedResourceIds = new List<int>();
            PeopleCount = 1; // Значение по умолчанию

            // Используем встроенные классы Command
            GoBackCommand = new Command(GoBack);
            ProceedToPaymentCommand = new Command(ProceedToPayment);
            SelectResourceCommand = new Command<BookingResourceViewModel>(SelectResource);

            LoadServiceDataAsync();
        }

        private async void LoadServiceDataAsync()
        {
            try
            {
                // Проверяем, что serviceId валидный
                if (_serviceId <= 0)
                {
                    ShowError("Ошибка", "Услуга не выбрана. Пожалуйста, выберите услугу из каталога.");
                    return;
                }

                // Загружаем услугу через сервис
                var service = await _catalogService.GetServiceDetailsAsync(_serviceId);
                if (service != null)
                {
                    Service = service;

                    // Инициализируем данные для отображения
                    InitializeServiceDisplayData();

                    // Устанавливаем значения по умолчанию
                    SelectedDate = DateTime.Today.AddDays(1);

                    // Обновляем временные слоты
                    UpdateTimeSlots();

                    // Обновляем доступные ресурсы
                    try
                    {
                        await UpdateAvailableResourcesAsync();
                    }
                    catch (Exception ex)
                    {
                        // Показываем ошибку, но продолжаем работу
                        ShowError("Предупреждение", "Не удалось загрузить доступные ресурсы: " + ex.Message);
                    }

                    // Рассчитываем цены
                    CalculatePrices();
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки услуги", ex.Message);
            }
        }


        private void InitializeServiceDisplayData()
        {
            // Определяем иконку и категорию по названию услуги
            var serviceName = Service.Name.ToLower();

            if (serviceName.Contains("боулинг"))
            {
                ServiceIcon = "🎳";
                ServiceCategory = "Боулинг";
            }
            else if (serviceName.Contains("караоке"))
            {
                ServiceIcon = "🎤";
                ServiceCategory = "Караоке";
            }
            else if (serviceName.Contains("тир"))
            {
                ServiceIcon = "🎯";
                ServiceCategory = "Тир";
            }
            else if (serviceName.Contains("бильярд"))
            {
                ServiceIcon = "🎱";
                ServiceCategory = "Бильярд";
            }
            else
            {
                ServiceIcon = "🎪";
                ServiceCategory = "Развлечения";
            }

            // Максимальная вместимость
            MaxCapacity = Service.Resources.Any()
                ? Service.Resources.Max(r => r.Capacity)
                : 10;

            // Текущая цена (будет пересчитываться при выборе даты)
            CurrentPrice = Service.WeekdayPrice;
        }

        private void UpdateTimeSlots()
        {
            TimeSlots.Clear();

            if (Service == null || !SelectedDate.HasValue)
                return;

            var startTime = SelectedDate.Value.Date.Add(Service.StartTime);
            var endTime = SelectedDate.Value.Date.Add(Service.EndTime);
            var current = startTime;

            while (current.TimeOfDay < Service.EndTime)
            {
                TimeSlots.Add(current);
                current = current.AddMinutes(30);
            }

            if (TimeSlots.Any())
                SelectedTimeSlot = TimeSlots.First();
        }

        private async Task UpdateAvailableResourcesAsync()
        {
            // Защита от параллельных вызовов
            if (_isLoadingResources)
                return;

            try
            {
                _isLoadingResources = true;

                AvailableResources.Clear();

                if (Service == null || !SelectedDate.HasValue || !SelectedTimeSlot.HasValue ||
                    PeopleCount <= 0)
                    return;

                int peopleCount = PeopleCount;

                // Конвертируем локальное время в UTC перед отправкой в БД
                var localDateTime = SelectedTimeSlot.Value;
                var utcDateTime = localDateTime.ToUniversalTime();

                // Используем новый метод, возвращающий BLL модели
                var resources = await _bookingService.FindAvailableResourceModelsAsync(
                    Service.Id,
                    utcDateTime, // Используем UTC время
                    Service.Duration,
                    peopleCount);

                // Очищаем коллекцию перед добавлением новых ресурсов
                AvailableResources.Clear();

                // Добавляем только уникальные ресурсы (по Id)
                var addedResourceIds = new HashSet<int>();
                foreach (var resource in resources)
                {
                    if (!addedResourceIds.Contains(resource.Id))
                    {
                        addedResourceIds.Add(resource.Id);
                        AvailableResources.Add(new BookingResourceViewModel
                        {
                            Id = resource.Id,
                            Name = resource.Name,
                            Capacity = resource.Capacity,
                            Icon = GetResourceIcon(resource.Name),
                            IsSelected = SelectedResourceIds.Contains(resource.Id)
                        });
                    }
                }

                // Рассчитываем необходимое количество ресурсов
                CalculateRequiredResources(peopleCount);

                // Обновляем визуальное состояние ресурсов
                UpdateResourceVisualStates();
            }
            catch (Exception ex)
            {
                ShowError("Ошибка загрузки ресурсов", ex.Message);
            }
            finally
            {
                _isLoadingResources = false;
            }
        }

        private void CalculateRequiredResources(int peopleCount)
        {
            if (!AvailableResources.Any())
            {
                ResourcesDescription = "Нет доступных ресурсов";
                RequiredResourceCount = 0;
                return;
            }

            // Находим максимальную вместимость ресурса
            int maxCapacity = AvailableResources.Max(r => r.Capacity);

            if (maxCapacity == 0)
            {
                ResourcesDescription = "Ресурсы недоступны";
                RequiredResourceCount = 0;
                return;
            }

            // Рассчитываем необходимое количество ресурсов
            RequiredResourceCount = (int)Math.Ceiling((double)peopleCount / maxCapacity);

            if (RequiredResourceCount == 1)
            {
                ResourcesDescription = $"Для {peopleCount} человек необходимо выбрать 1 ресурс (вместимость: {maxCapacity} чел.)";
            }
            else
            {
                ResourcesDescription = $"Для {peopleCount} человек необходимо выбрать {RequiredResourceCount} ресурс(ов) (вместимость одного: {maxCapacity} чел.)";
            }
        }

        private string GetResourceIcon(string resourceName)
        {
            var name = resourceName.ToLower();

            if (name.Contains("дорожк")) return "🎳";
            if (name.Contains("комнат")) return "🎤";
            if (name.Contains("стол")) return "🎱";
            if (name.Contains("стенд")) return "🎯";
            if (name.Contains("экран")) return "📺";
            if (name.Contains("площадк")) return "⚽";

            return "📋";
        }

        private void SelectResource(BookingResourceViewModel resource)
        {
            if (resource == null) return;

            // Не позволяем выбирать отключенные ресурсы
            if (resource.IsDisabled)
            {
                return;
            }

            // Если ресурс уже выбран, снимаем выбор
            if (resource.IsSelected)
            {
                resource.IsSelected = false;
                SelectedResourceIds.Remove(resource.Id);
            }
            else
            {
                // Разрешаем выбирать ресурсы
                resource.IsSelected = true;
                SelectedResourceIds.Add(resource.Id);
            }

            // Обновляем визуальное состояние всех ресурсов
            UpdateResourceVisualStates();
            CalculatePrices();
        }

        private void UpdateResourceVisualStates()
        {
            int selectedCount = SelectedResourceIds.Count;

            foreach (var resource in AvailableResources)
            {
                // Если выбрано нужное количество, остальные ресурсы становятся неактивными
                if (selectedCount >= RequiredResourceCount && !resource.IsSelected)
                {
                    resource.IsDisabled = true;
                }
                else
                {
                    resource.IsDisabled = false;
                }
            }
        }

        private void CalculatePrices()
        {
            try
            {
                if (Service == null || !SelectedDate.HasValue || !SelectedTimeSlot.HasValue ||
                    PeopleCount <= 0)
                    return;

                // 1. Стоимость услуги
                bool isWeekend = SelectedDate.Value.DayOfWeek == DayOfWeek.Saturday ||
                                SelectedDate.Value.DayOfWeek == DayOfWeek.Sunday;

                decimal pricePerHour = isWeekend ? Service.WeekendPrice : Service.WeekdayPrice;
                decimal hours = Service.Duration / 60.0m;
                ServicePrice = pricePerHour * hours;

                // 2. Дополнительные услуги
                ExtrasPrice = 0;
                if (IncludeInstructor) ExtrasPrice += 500;
                if (IncludeFood) ExtrasPrice += 1000;

                // 3. Получаем скидку пользователя асинхронно (без Task.Run для избежания проблем с DbContext)
                _ = CalculateDiscountAsync(ServicePrice + ExtrasPrice);

                // 4. Предварительный расчет без скидки
                TotalPrice = ServicePrice + ExtrasPrice;
                DiscountAmount = 0;
            }
            catch (Exception ex)
            {
                ShowError("Ошибка расчета цены", ex.Message);
            }
        }

        private async Task CalculateDiscountAsync(decimal basePrice)
        {
            // Отменяем предыдущий вызов, если он еще выполняется
            _discountCalculationCancellation?.Cancel();
            _discountCalculationCancellation?.Dispose();
            _discountCalculationCancellation = new CancellationTokenSource();

            var cancellationToken = _discountCalculationCancellation.Token;

            try
            {
                var discount = await _userService.GetUserDiscountAsync(_currentUserId).ConfigureAwait(false);

                // Проверяем, не был ли запрос отменен
                cancellationToken.ThrowIfCancellationRequested();

                // Скидка в БД хранится как проценты (15.00 = 15%), преобразуем в десятичную дробь
                var discountDecimal = discount / 100m;

                // Ограничиваем скидку разумными пределами (максимум 100%)
                if (discountDecimal > 1m)
                {
                    discountDecimal = 1m;
                }
                if (discountDecimal < 0m)
                {
                    discountDecimal = 0m;
                }

                UserDiscount = discountDecimal;
                var newDiscountAmount = basePrice * discountDecimal;
                var newTotalPrice = basePrice - newDiscountAmount;

                // Проверяем, что скидка не отрицательная и не слишком большая
                if (newDiscountAmount < 0 || newDiscountAmount > basePrice)
                {
                    newDiscountAmount = 0;
                    newTotalPrice = basePrice;
                }

                // Обновляем UI через Dispatcher (возвращаемся в UI поток)
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Проверяем еще раз перед обновлением UI
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        DiscountAmount = newDiscountAmount;
                        TotalPrice = newTotalPrice;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Игнорируем отмену - это нормально, если начался новый расчет
            }
            catch (Exception)
            {
                // Если не удалось получить скидку, используем 0
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            DiscountAmount = 0;
                            TotalPrice = basePrice;
                        }
                    });
                }
            }
        }

        private void GoBack()
        {
            // Навигация назад - можно использовать события или сервис навигации
            OnGoBackRequested?.Invoke(this, EventArgs.Empty);
        }

        private async void ProceedToPayment()
        {
            try
            {
                // Валидация
                if (!ValidateInput())
                    return;

                int peopleCount = PeopleCount;
                if (peopleCount <= 0)
                {
                    ShowError("Ошибка", "Количество человек должно быть больше 0");
                    return;
                }

                // Проверяем, что ресурсы выбраны
                if (SelectedResourceIds == null || SelectedResourceIds.Count == 0)
                {
                    ShowError("Ошибка", "Необходимо выбрать хотя бы один ресурс");
                    return;
                }

                // Проверяем, что Service загружен
                if (Service == null || Service.Id <= 0)
                {
                    ShowError("Ошибка", "Услуга не загружена. Пожалуйста, обновите страницу.");
                    return;
                }

                // Конвертируем локальное время в UTC
                var utcDateTime = SelectedTimeSlot.Value.ToUniversalTime();

                // Логируем данные для отладки
                System.Diagnostics.Debug.WriteLine($"Создание бронирования:");
                System.Diagnostics.Debug.WriteLine($"  UserId: {_currentUserId}");
                System.Diagnostics.Debug.WriteLine($"  ServiceId: {Service.Id}");
                System.Diagnostics.Debug.WriteLine($"  BookingDateTime (UTC): {utcDateTime}");
                System.Diagnostics.Debug.WriteLine($"  DurationMinutes: {Service.Duration}");
                System.Diagnostics.Debug.WriteLine($"  PeopleCount: {peopleCount}");
                System.Diagnostics.Debug.WriteLine($"  SelectedResourceIds: [{string.Join(", ", SelectedResourceIds)}]");
                System.Diagnostics.Debug.WriteLine($"  IncludeInstructor: {IncludeInstructor}");
                System.Diagnostics.Debug.WriteLine($"  IncludeEquipment: {IncludeEquipment}");
                System.Diagnostics.Debug.WriteLine($"  IncludeFood: {IncludeFood}");

                // Создаем заказ через сервис бронирования
                var bookingRequest = new BookingService.BookingRequest
                {
                    UserId = _currentUserId,
                    ServiceId = Service.Id,
                    BookingDateTime = utcDateTime, // Используем UTC
                    DurationMinutes = Service.Duration,
                    PeopleCount = peopleCount,
                    SelectedResourceIds = new List<int>(SelectedResourceIds), // Создаем копию списка
                    IncludeInstructor = IncludeInstructor,
                    IncludeEquipment = IncludeEquipment,
                    IncludeFood = IncludeFood
                };

                var order = await _bookingService.CreateBookingAsync(bookingRequest);

                if (order != null)
                {
                    MessageBox.Show("Бронирование успешно создано!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    // Навигация на страницу заказов
                    GoBack();
                }
                else
                {
                    ShowError("Ошибка", "Не удалось создать бронирование");
                }
            }
            catch (Exception ex)
            {
                // Показываем более детальное сообщение об ошибке
                var errorMessage = $"Не удалось создать бронирование.\n\nДетали ошибки:\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nВнутренняя ошибка:\n{ex.InnerException.Message}";
                }
                ShowError("Ошибка создания бронирования", errorMessage);

                // Логируем для отладки
                System.Diagnostics.Debug.WriteLine($"Ошибка создания бронирования: {ex}");
            }
        }

        private bool ValidateInput()
        {
            if (PeopleCount <= 0)
            {
                ShowError("Ошибка", "Введите количество человек");
                return false;
            }

            if (!SelectedDate.HasValue)
            {
                ShowError("Ошибка", "Выберите дату");
                return false;
            }

            if (!SelectedTimeSlot.HasValue)
            {
                ShowError("Ошибка", "Выберите время");
                return false;
            }

            if (SelectedResourceIds.Count != RequiredResourceCount)
            {
                ShowError("Ошибка", $"Необходимо выбрать ровно {RequiredResourceCount} ресурс(ов) для {PeopleCount} человек");
                return false;
            }

            return true;
        }

        private void ShowError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        // Событие для навигации
        public event EventHandler OnGoBackRequested;

        // INotifyPropertyChanged реализация
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

    // Простая ViewModel для ресурсов в бронировании
    public class BookingResourceViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isDisabled;

        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public string Icon { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsDisabled
        {
            get => _isDisabled;
            set
            {
                if (_isDisabled != value)
                {
                    _isDisabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}