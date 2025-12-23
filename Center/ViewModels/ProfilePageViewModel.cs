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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Center.ViewModels
{
    public class ProfilePageViewModel : INotifyPropertyChanged
    {
        private readonly UserService _userService;
        private readonly AdminCRUDService _adminCRUDService;
        private readonly int _userId;

        private string _userName;
        private string _userEmail;
        private string _citizenCategory;
        private decimal _discount;
        private string _userRole;
        private string _password;
        private bool _isPasswordVisible;
        private string _passwordDisplay;
        private int _citizenCategoryId;
        private bool _isCategoryEditing;
        private ObservableCollection<UserService.CitizenCategoryModel> _allCitizenCategories;
        private UserService.CitizenCategoryModel _selectedCitizenCategory;

        public string UserName
        {
            get => _userName;
            set => SetField(ref _userName, value);
        }

        public string UserEmail
        {
            get => _userEmail;
            set => SetField(ref _userEmail, value);
        }

        public string CitizenCategory
        {
            get => _citizenCategory;
            set => SetField(ref _citizenCategory, value);
        }

        public decimal Discount
        {
            get => _discount;
            set
            {
                if (SetField(ref _discount, value))
                {
                    OnPropertyChanged(nameof(DiscountPercent));
                }
            }
        }

        public int DiscountPercent
        {
            get => (int)Math.Round(_discount);
        }

        public string UserRole
        {
            get => _userRole;
            set => SetField(ref _userRole, value);
        }

        public string PasswordDisplay
        {
            get => _passwordDisplay;
            set => SetField(ref _passwordDisplay, value);
        }

        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                if (SetField(ref _isPasswordVisible, value))
                {
                    UpdatePasswordDisplay();
                }
            }
        }

        public bool IsCategoryEditing
        {
            get => _isCategoryEditing;
            set => SetField(ref _isCategoryEditing, value);
        }

        public ObservableCollection<UserService.CitizenCategoryModel> AllCitizenCategories
        {
            get => _allCitizenCategories;
            set => SetField(ref _allCitizenCategories, value);
        }

        public UserService.CitizenCategoryModel SelectedCitizenCategory
        {
            get => _selectedCitizenCategory;
            set => SetField(ref _selectedCitizenCategory, value);
        }

        public ICommand TogglePasswordVisibilityCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand SaveCategoryCommand { get; }

        public ProfilePageViewModel(UserService userService, AdminCRUDService adminCRUDService, int userId)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _adminCRUDService = adminCRUDService ?? throw new ArgumentNullException(nameof(adminCRUDService));
            _userId = userId;

            AllCitizenCategories = new ObservableCollection<UserService.CitizenCategoryModel>();

            // Используем встроенные классы Command вместо RelayCommand
            TogglePasswordVisibilityCommand = new Command(TogglePasswordVisibility);
            ChangePasswordCommand = new Command(ChangePassword);
            EditCategoryCommand = new Command(EditCategory);
            SaveCategoryCommand = new Command(SaveCategory);

            _ = LoadUserProfileAndCategoriesAsync();
        }

        private async Task LoadUserProfileAndCategoriesAsync()
        {
            try
            {
                // Загружаем профиль пользователя
                var user = await _userService.GetUserProfileAsync(_userId).ConfigureAwait(false);
                if (user != null)
                {
                    // Получаем категорию пользователя из БД для получения ID
                    var allUsers = await _adminCRUDService.GetAllUsersAsync().ConfigureAwait(false);
                    var dbUser = allUsers.FirstOrDefault(u => u.userid == _userId);
                    _citizenCategoryId = dbUser?.citizencategoryid ?? 0;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        UserName = user.Name;
                        UserEmail = user.Email;
                        CitizenCategory = user.CitizenCategory ?? "Не указана";
                        UserRole = user.Role == "admin" ? "Администратор" : 
                                  user.Role == "client" ? "Клиент" : user.Role;
                        _password = user.Passwordhash ?? "";
                        UpdatePasswordDisplay();
                    });

                    // Загружаем скидку отдельно
                    var discount = await _userService.GetUserDiscountAsync(_userId).ConfigureAwait(false);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Discount = discount;
                    });
                }

                // Загружаем категории граждан
                var categories = await _userService.GetAllCitizenCategoriesAsync().ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AllCitizenCategories.Clear();
                    foreach (var category in categories)
                    {
                        AllCitizenCategories.Add(category);
                    }
                    // Устанавливаем текущую категорию как выбранную
                    if (_citizenCategoryId > 0)
                    {
                        SelectedCitizenCategory = AllCitizenCategories.FirstOrDefault(c => c.Id == _citizenCategoryId);
                    }
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка загрузки данных: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void EditCategory()
        {
            IsCategoryEditing = true;
            // Устанавливаем текущую категорию как выбранную
            if (_citizenCategoryId > 0)
            {
                SelectedCitizenCategory = AllCitizenCategories.FirstOrDefault(c => c.Id == _citizenCategoryId);
            }
        }

        private async void SaveCategory()
        {
            if (SelectedCitizenCategory == null)
            {
                MessageBox.Show("Пожалуйста, выберите категорию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var success = await _userService.ChangeUserCitizenCategoryAsync(_userId, SelectedCitizenCategory.Id).ConfigureAwait(false);
                
                if (success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _citizenCategoryId = SelectedCitizenCategory.Id;
                        CitizenCategory = SelectedCitizenCategory.Name;
                        IsCategoryEditing = false;
                        
                        // Обновляем скидку
                        _ = UpdateDiscountAsync();
                        
                        MessageBox.Show("Категория успешно изменена!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Ошибка изменения категории", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка сохранения категории: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async Task UpdateDiscountAsync()
        {
            try
            {
                var discount = await _userService.GetUserDiscountAsync(_userId).ConfigureAwait(false);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Discount = discount;
                });
            }
            catch (Exception ex)
            {
                // Игнорируем ошибку обновления скидки
            }
        }

        private void UpdatePasswordDisplay()
        {
            if (IsPasswordVisible)
            {
                PasswordDisplay = _password;
            }
            else
            {
                PasswordDisplay = new string('●', _password?.Length ?? 0);
            }
        }

        private void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private void ChangePassword()
        {
            var dialog = new Center.Views.ChangePasswordDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.NewPassword))
            {
                _ = ChangePasswordAsync(dialog.NewPassword);
            }
        }

        private async Task ChangePasswordAsync(string newPassword)
        {
            try
            {
                var user = await _userService.GetUserProfileAsync(_userId).ConfigureAwait(false);
                if (user != null)
                {
                    // Получаем категорию пользователя из БД
                    var allUsers = await _adminCRUDService.GetAllUsersAsync().ConfigureAwait(false);
                    var dbUser = allUsers.FirstOrDefault(u => u.userid == _userId);
                    int citizenCategoryId = dbUser?.citizencategoryid ?? 1;

                    var userData = new UserData
                    {
                        Name = user.Name,
                        Email = user.Email,
                        Password = newPassword,
                        Role = user.Role,
                        CitizenCategoryId = citizenCategoryId
                    };

                    await _adminCRUDService.UpdateUserAsync(_userId, userData).ConfigureAwait(false);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _password = newPassword;
                        UpdatePasswordDisplay();
                        MessageBox.Show("Пароль успешно изменен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Ошибка изменения пароля: {ex.Message}",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
