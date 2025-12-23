using System;
using System.Windows;
using System.Windows.Input;
using Center.ViewModels;
using System.Linq;

namespace Center.Views
{
    public class OrderDetailsViewModel
    {
        private readonly OrderViewModel _order;
        private readonly Window _window;

        public int OrderId => _order.Id;
        public string ServiceName => _order.ServiceName;
        public string FormattedOrderDate => _order.OrderDate.ToString("dd.MM.yyyy HH:mm");
        public int PeopleCount => _order.PeopleCount;
        public string FormattedTotalPrice => $"{_order.TotalPrice:N2} ₽";
        public string Status => _order.Status;
        public string StatusColor => _order.StatusColor;
        public string ResourcesSummary => _order.ResourcesSummary;
        public string TimeUntilStart => _order.TimeUntilStart;
        public bool CanSetReminder => _order.CanSetReminder;
        public bool ShowTimer => _order.ShowTimer;

        public ICommand CloseCommand { get; }
        public ICommand SetReminderCommand { get; }

        public OrderDetailsViewModel(Window window, OrderViewModel order)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _order = order ?? throw new ArgumentNullException(nameof(order));

            CloseCommand = new Command(CloseWindow);
            SetReminderCommand = new Command(SetReminder);
        }

        private void CloseWindow()
        {
            _window.Close();
        }

        private void SetReminder()
        {
            MessageBox.Show(
                $"Напоминание для заказа #{_order.Id} установлено!\n" +
                $"Время начала: {_order.OrderDate:dd.MM.yyyy HH:mm}",
                "Напоминание",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // Встроенный класс Command
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