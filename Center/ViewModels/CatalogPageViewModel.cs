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
    public class CatalogPageViewModel : INotifyPropertyChanged
    {
        private readonly CatalogService _catalogService;
        private ObservableCollection<ServiceModel> _services;
        private ObservableCollection<ServiceModel> _displayedServices;
        private ServiceModel _selectedService;
        private bool _isLoading;
        private string _searchText;
        private ObservableCollection<CategoryModel> _allCategories;
        private CategoryModel _selectedCategory;

        public event PropertyChangedEventHandler PropertyChanged;
        public event Action<int> OnNavigateToBooking;

        public ObservableCollection<ServiceModel> Services
        {
            get => _services;
            set
            {
                _services = value;
                OnPropertyChanged();
            }
        }

        public ServiceModel SelectedService
        {
            get => _selectedService;
            set
            {
                _selectedService = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ServiceModel> DisplayedServices
        {
            get => _displayedServices;
            set
            {
                _displayedServices = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                {
                    FilterServices();
                }
            }
        }

        public ObservableCollection<CategoryModel> AllCategories
        {
            get => _allCategories;
            set
            {
                _allCategories = value;
                OnPropertyChanged();
            }
        }

        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetField(ref _selectedCategory, value))
                {
                    FilterServices();
                }
            }
        }

        public ICommand BookServiceCommand { get; }
        public ICommand SearchCommand { get; }

        public CatalogPageViewModel(CatalogService catalogService)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
            Services = new ObservableCollection<ServiceModel>();
            DisplayedServices = new ObservableCollection<ServiceModel>();
            AllCategories = new ObservableCollection<CategoryModel>();

            // Используем стандартные команды без RelayCommand
            BookServiceCommand = new Command<ServiceModel>(BookService, CanBookService);
            SearchCommand = new Command(FilterServices);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => IsLoading = true);

                // Загружаем категории первыми
                await LoadCategoriesAsync();

                // Затем загружаем услуги
                await LoadServicesAsync();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            }
        }

        private async Task LoadServicesAsync()
        {
            try
            {
                // Используем GetFilteredServicesAsync который возвращает List<Services>
                var services = await _catalogService.GetFilteredServicesAsync().ConfigureAwait(false);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Services.Clear();
                    DisplayedServices.Clear();
                    foreach (var service in services)
                    {
                        Services.Add(service);
                    }
                    FilterServices();
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки услуг: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _catalogService.GetAllCategoriesAsync().ConfigureAwait(false);

                // Обновляем коллекцию в UI потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AllCategories.Clear();
                    // Добавляем элемент-подсказку "Сортировать по"
                    AllCategories.Add(new CategoryModel { Id = -1, Name = "Сортировать по..." });
                    // Добавляем пустую категорию "Все"
                    AllCategories.Add(new CategoryModel { Id = 0, Name = "Все категории" });
                    foreach (var category in categories)
                    {
                        AllCategories.Add(category);
                    }
                    // Устанавливаем элемент-подсказку как выбранный по умолчанию
                    SelectedCategory = AllCategories.FirstOrDefault(c => c.Id == -1);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void FilterServices()
        {
            if (Services == null || Services.Count == 0)
                return;

            DisplayedServices.Clear();

            var filtered = Services.AsEnumerable();

            // Фильтр по категории (игнорируем элемент-подсказку с Id = -1)
            if (SelectedCategory != null && SelectedCategory.Id > 0)
            {
                filtered = filtered.Where(s => s.Categories != null &&
                    s.Categories.Any(c => c.Id == SelectedCategory.Id));
            }

            // Фильтр по поисковому запросу
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(s => s.Name.ToLower().Contains(searchLower));
            }

            foreach (var service in filtered)
            {
                DisplayedServices.Add(service);
            }
        }

        // Метод для бронирования услуги
        public void BookService(ServiceModel service)
        {
            if (service != null && service.Id > 0)
            {
                OnNavigateToBooking?.Invoke(service.Id);
            }
        }

        private bool CanBookService(ServiceModel service)
        {
            return service != null && service.Id > 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
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

        // Простой класс Command для замены RelayCommand
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

        // Простой класс Command<T> для типизированных команд
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
