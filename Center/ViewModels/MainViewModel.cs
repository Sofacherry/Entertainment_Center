using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

        public ICommand NavigateToCatalogCommand => new SimpleCommand(NavigateToCatalog);
        public ICommand NavigateToBookingCommand => new SimpleCommand(NavigateToBooking);
        public ICommand NavigateToOrdersCommand => new SimpleCommand(NavigateToOrders);
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

        public MainViewModel(CatalogService catalogService, UserService userService)
        {
            _catalogService = catalogService;
            _userService = userService;

            NavigateToCatalog();
        }

        private async void LoadCurrentUser()
        {
            try
            {
                var user = await _userService.GetUserProfileAsync(1);
                if (user != null)
                {
                    CurrentUser = user;
                    IsAdmin = user.Role == "admin";
                }
                else
                {
                    CurrentUser = new UserModel { Name = "Гость", Role = "client" };
                }
            }
            catch
            {
                CurrentUser = new UserModel { Name = "Ошибка загрузки", Role = "client" };
            }
        }

        private void NavigateToCatalog()
        {
            try
            {
                var catalogView = new CatalogPageView();
                var catalogViewModel = new CatalogPageViewModel(_catalogService);
                catalogView.DataContext = catalogViewModel;

                CurrentPage = catalogView;
                CurrentPageTitle = "Каталог услуг";
            }
            catch (Exception ex)
            {
                CurrentPage = CreateErrorPage($"Ошибка: {ex.Message}");
            }
        }

        private void NavigateToBooking()
        {
            CurrentPageTitle = "Бронирование";
            CurrentPage = CreateMessagePage("Страница бронирования\n(в разработке)");
        }

        private void NavigateToOrders()
        {
            CurrentPageTitle = "Мои заказы";
            CurrentPage = CreateMessagePage("Мои заказы\n(в разработке)");
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
            var info = $"Имя: {CurrentUser?.Name ?? "Неизвестно"}\n" +
                      $"Роль: {CurrentUser?.Role ?? "Гость"}";
            MessageBox.Show(info, "Профиль");
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