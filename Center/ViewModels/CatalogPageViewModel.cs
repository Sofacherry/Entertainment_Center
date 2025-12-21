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

namespace Center.ViewModels
{
    public class CatalogPageViewModel : INotifyPropertyChanged
    {
        private readonly CatalogService _catalogService;
        private ObservableCollection<ServiceModel> _allServices; // Все услуги
        private ObservableCollection<ServiceModel> _displayedServices; // Отображаемые услуги
        private ObservableCollection<CategoryModel> _allCategories;
        private ObservableCollection<CategoryModel> _selectedCategories;
        private string _searchText;
        private bool _isLoading;
        private bool _hasSelectedCategories;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<ServiceModel> AllServices
        {
            get => _allServices;
            set
            {
                _allServices = value;
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

        private string _selectedCategoryText = "Сортировать по";
        public string SelectedCategoryText
        {
            get => _selectedCategoryText;
            set
            {
                _selectedCategoryText = value;
                OnPropertyChanged();
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

        public ObservableCollection<CategoryModel> SelectedCategories
        {
            get => _selectedCategories;
            set
            {
                _selectedCategories = value;
                OnPropertyChanged();
                HasSelectedCategories = value != null && value.Count > 0;
                ApplyFilters();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilters();
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

        public bool HasSelectedCategories
        {
            get => _hasSelectedCategories;
            set
            {
                _hasSelectedCategories = value;
                OnPropertyChanged();
            }
        }

        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged();

                if (value != null)
                {
                    if (value.Id == -1)
                    {
                        SelectedCategories.Clear();
                        DisplayAllServices();
                    }
                    else if (!SelectedCategories.Contains(value))
                    {
                        SelectedCategories.Add(value);
                        ApplyFilters();
                    }

                }
            }
        }



        // Команды
        public ICommand SearchCommand { get; }
        public ICommand BookServiceCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand RemoveCategoryCommand { get; }
        public ICommand AddCategoryCommand { get; }

        public CatalogPageViewModel(CatalogService catalogService)
        {
            _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));

            AllServices = new ObservableCollection<ServiceModel>();
            DisplayedServices = new ObservableCollection<ServiceModel>();
            AllCategories = new ObservableCollection<CategoryModel>();
            SelectedCategories = new ObservableCollection<CategoryModel>();

            SearchCommand = new RelayCommand(SearchServices);
            BookServiceCommand = new RelayCommand<ServiceModel>(BookService);
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            RemoveCategoryCommand = new RelayCommand<CategoryModel>(RemoveCategory);
            AddCategoryCommand = new RelayCommand<CategoryModel>(AddCategory);

            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var categories = await _catalogService.GetAllCategoriesAsync();

                var services = await _catalogService.GetAllServicesAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    AllCategories.Clear();

                    AllCategories.Add(new CategoryModel
                    {
                        Id = -1, 
                        Name = "Все услуги"
                    });

                    foreach (var category in categories)
                    {
                        AllCategories.Add(new CategoryModel
                        {
                            Id = category.categoryid,
                            Name = category.categoryname
                        });
                    }
                    AllServices.Clear();
                    foreach (var service in services)
                    {
                        AllServices.Add(service);
                    }

                    DisplayAllServices();

                    SelectedCategory = AllCategories.FirstOrDefault();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                              "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
        private void DisplayAllServices()
        {
            DisplayedServices.Clear();
            foreach (var service in AllServices)
            {
                DisplayedServices.Add(service);
            }
        }

        private void ApplyFilters()
        {
            if (AllServices == null) return;

            var filtered = AllServices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(s =>
                    s.Name.ToLower().Contains(searchLower));
            }

            if (SelectedCategories != null && SelectedCategories.Count > 0)
            {
                var selectedCategoryIds = SelectedCategories.Select(c => c.Id).ToList();
                filtered = filtered.Where(s =>
                    s.Categories != null &&
                    s.Categories.Any(c => selectedCategoryIds.Contains(c.Id)));
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                DisplayedServices.Clear();
                foreach (var service in filtered)
                {
                    DisplayedServices.Add(service);
                }
            });
        }

        private void SearchServices()
        {
            ApplyFilters();
        }

        private void BookService(ServiceModel service)
        {
            if (service != null)
            {
                MessageBox.Show($"Бронирование услуги: {service.Name}\nЦена: {service.WeekdayPrice:N0} ₽ (будни)",
                              "Бронирование", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategories.Clear();
            DisplayAllServices(); 
        }

        private void RemoveCategory(CategoryModel category)
        {
            if (category != null && SelectedCategories.Contains(category))
            {
                SelectedCategories.Remove(category);
                ApplyFilters(); 
            }
        }

        private void AddCategory(CategoryModel category)
        {
            if (category != null && !SelectedCategories.Contains(category))
            {
                SelectedCategories.Add(category);
                ApplyFilters(); 
            }
        }

        public void AddSelectedCategory(CategoryModel category)
        {
            AddCategory(category);
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);
    }
}