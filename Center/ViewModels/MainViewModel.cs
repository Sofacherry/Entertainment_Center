using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Center.Views;
using BLL.Models;
using BLL;

namespace Center.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly CatalogService _catalogService;
        private readonly UserService _userService;
        private readonly BookingService _bookingService;

        private UserModel _currentUser;
        public UserModel CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        private UserControl _currentPage;
        public UserControl CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
            }
        }

        private string _currentPageTitle = "Каталог услуг";
        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set
            {
                _currentPageTitle = value;
                OnPropertyChanged();
            }
        }

        private bool _isAdmin;
        public bool IsAdmin
        {
            get => _isAdmin;
            set
            {
                _isAdmin = value;
                OnPropertyChanged();
            }
        }

        private bool _isCatalogSelected = true;
        public bool IsCatalogSelected
        {
            get => _isCatalogSelected;
            set => SetField(ref _isCatalogSelected, value);
        }

        private bool _isBookingSelected;
        public bool IsBookingSelected
        {
            get => _isBookingSelected;
            set => SetField(ref _isBookingSelected, value);
        }

        private bool _isOrdersSelected;
        public bool IsOrdersSelected
        {
            get => _isOrdersSelected;
            set => SetField(ref _isOrdersSelected, value);
        }

        private bool _isProfileSelected;
        public bool IsProfileSelected
        {
            get => _isProfileSelected;
            set => SetField(ref _isProfileSelected, value);
        }

        private bool _isReportsSelected;
        public bool IsReportsSelected
        {
            get => _isReportsSelected;
            set => SetField(ref _isReportsSelected, value);
        }

        public ICommand NavigateToCatalogCommand => new SimpleCommand(NavigateToCatalog);
        public ICommand NavigateToBookingCommand => new SimpleCommand(NavigateToBooking);
        public ICommand NavigateToOrdersCommand => new SimpleCommand(NavigateToOrders);
        public ICommand NavigateToReportsCommand => new SimpleCommand(NavigateToReports);
        public ICommand QuickBookingCommand => new SimpleCommand(QuickBooking);
        public ICommand ShowProfileCommand => new SimpleCommand(ShowProfile);
        public ICommand LogoutCommand => new SimpleCommand(Logout);

        private class SimpleCommand : ICommand
        {
            private readonly Action _action;

            public SimpleCommand(Action action) => _action = action;

            public bool CanExecute(object parameter) => true;
            public void Execute(object parameter) => _action();
            public event EventHandler CanExecuteChanged;
        }

        public MainViewModel(CatalogService catalogService, UserService userService,
                           BookingService bookingService)
        {
            _catalogService = catalogService;
            _userService = userService;
            _bookingService = bookingService;

            // Загружаем пользователя синхронно, чтобы избежать конфликтов с DbContext
            _ = LoadCurrentUserAsync();
            NavigateToCatalog();
        }

        private async Task LoadCurrentUserAsync()
        {
            try
            {
                var user = await _userService.GetUserProfileAsync(1).ConfigureAwait(false);
                if (user != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentUser = user;
                        IsAdmin = user.Role == "admin";
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentUser = new UserModel { Name = "Гость", Role = "client" };
                    });
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentUser = new UserModel { Name = "Ошибка загрузки", Role = "client" };
                });
            }
        }

        // Метод для отображения каталога услуг
        private void NavigateToCatalog()
        {
            try
            {
                var catalogView = new CatalogPageView();
                var catalogViewModel = new CatalogPageViewModel(_catalogService);
                catalogView.DataContext = catalogViewModel;
                catalogViewModel.OnNavigateToBooking += NavigateToBookingWithService;

                CurrentPage = catalogView;
                CurrentPageTitle = "Каталог услуг";

                IsCatalogSelected = true;
                IsBookingSelected = false;
                IsOrdersSelected = false;
                IsProfileSelected = false;
                IsReportsSelected = false;
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        // Метод для перехода на страницу бронирования (без выбранной услуги)
        private void NavigateToBooking()
        {
            try
            {
                var bookingView = new BookingPageView();
                var bookingViewModel = new BookingViewModel(
                    _bookingService,
                    _userService,
                    _catalogService,
                    0,
                    CurrentUser?.Id ?? 1);
                bookingView.DataContext = bookingViewModel;
                bookingViewModel.OnGoBackRequested += (s, e) => NavigateToCatalog();

                CurrentPage = bookingView;
                CurrentPageTitle = "Бронирование";

                IsCatalogSelected = false;
                IsBookingSelected = true;
                IsOrdersSelected = false;
                IsProfileSelected = false;
                IsReportsSelected = false;
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        // Метод для перехода на страницу бронирования с выбранной услугой
        private void NavigateToBookingWithService(int serviceId)
        {
            try
            {
                var bookingView = new BookingPageView();
                var bookingViewModel = new BookingViewModel(
                    _bookingService,
                    _userService,
                    _catalogService,
                    serviceId,
                    CurrentUser?.Id ?? 1);

                bookingView.DataContext = bookingViewModel;
                bookingViewModel.OnGoBackRequested += (s, e) => NavigateToCatalog();

                CurrentPage = bookingView;
                CurrentPageTitle = "Бронирование услуги";

                IsCatalogSelected = false;
                IsBookingSelected = true;
                IsOrdersSelected = false;
                IsProfileSelected = false;
                IsReportsSelected = false;
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        private void NavigateToOrders()
        {
            try
            {
                var ordersView = new CurrentOrdersPageView();
                var ordersViewModel = new CurrentOrdersViewModel(_bookingService, CurrentUser?.Id ?? 1);
                ordersView.DataContext = ordersViewModel;

                CurrentPage = ordersView;
                CurrentPageTitle = "Мои заказы";

                IsCatalogSelected = false;
                IsBookingSelected = false;
                IsOrdersSelected = true;
                IsProfileSelected = false;
                IsReportsSelected = false;
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        private void NavigateToReports()
        {
            try
            {
                var reportsView = new ReportsPageView();
                var reportsViewModel = new ReportsPageViewModel();
                reportsView.DataContext = reportsViewModel;

                CurrentPage = reportsView;
                CurrentPageTitle = "Отчеты";

                IsCatalogSelected = false;
                IsBookingSelected = false;
                IsOrdersSelected = false;
                IsProfileSelected = false;
                IsReportsSelected = true;
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        private UserControl CreateMessagePage(string message)
        {
            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            var grid = new Grid();
            grid.Children.Add(textBlock);

            return new UserControl { Content = grid };
        }

        private UserControl CreateErrorPage(string error)
        {
            var textBlock = new TextBlock
            {
                Text = $"❌ {error}",
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = System.Windows.Media.Brushes.Red
            };

            var grid = new Grid();
            grid.Children.Add(textBlock);

            return new UserControl { Content = grid };
        }

        private void QuickBooking()
        {
            MessageBox.Show("Быстрое бронирование", "Информация");
        }

        private void ShowProfile()
        {
            try
            {
                var profileView = new ProfilePageView();
                var adminCRUDService = App.ServiceProvider.GetRequiredService<AdminCRUDService>();
                var profileViewModel = new ProfilePageViewModel(
                    _userService,
                    adminCRUDService,
                    CurrentUser?.Id ?? 1);
                profileView.DataContext = profileViewModel;

                CurrentPage = profileView;
                CurrentPageTitle = "Профиль";

                IsCatalogSelected = false;
                IsBookingSelected = false;
                IsOrdersSelected = false;
                IsProfileSelected = true;
                IsReportsSelected = false;
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        private void Logout()
        {
            if (MessageBox.Show("Выйти из системы?", "Выход",
                MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
