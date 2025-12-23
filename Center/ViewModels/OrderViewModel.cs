using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using BLL.Models;

namespace Center.ViewModels
{
    public class OrderViewModel : INotifyPropertyChanged
    {
        private readonly OrderModel _orderModel;

        public int Id => _orderModel.Id;
        public int UserId => _orderModel.UserId;
        public string UserName => _orderModel.UserName;
        public int ServiceId => _orderModel.ServiceId;
        public string ServiceName => _orderModel.ServiceName;
        public DateTime OrderDate => _orderModel.OrderDate.ToLocalTime(); // Конвертируем из UTC
        public decimal TotalPrice => _orderModel.TotalPrice;
        public int PeopleCount => _orderModel.PeopleCount;
        public string Status => _orderModel.Status;
        public List<ResourceModel> Resources => _orderModel.Resources;
        public bool CanDelete => CanOrderBeDeleted();

        // Дополнительные свойства для отображения
        public string ServiceIcon => GetServiceIcon(ServiceName);
        public string StatusColor => _orderModel.StatusColor;
        public string ResourcesSummary => GetResourcesSummary();
        public string TimeUntilStart => CalculateTimeUntilStart();
        public bool ShowTimer => ShouldShowTimer();
        public bool CanCancel => CanOrderBeCancelled();
        public bool CanSetReminder => CanSetReminderForOrder();
        public bool IsActive => IsOrderActive();

        public OrderViewModel(OrderModel orderModel)
        {
            _orderModel = orderModel ?? throw new ArgumentNullException(nameof(orderModel));
        }

        private string GetServiceIcon(string serviceName)
        {
            var name = serviceName?.ToLower() ?? "";

            if (name.Contains("боулинг")) return "🎳";
            if (name.Contains("караоке")) return "🎤";
            if (name.Contains("тир")) return "🎯";
            if (name.Contains("бильярд")) return "🎱";
            if (name.Contains("кино")) return "🎬";
            if (name.Contains("бассейн")) return "🏊";
            if (name.Contains("спорт")) return "⚽";

            return "🎪";
        }

        private string GetResourcesSummary()
        {
            if (Resources == null || !Resources.Any())
                return "Ресурсы не выбраны";

            var resourceNames = Resources.Select(r => r.Name);
            return string.Join(", ", resourceNames);
        }

        private string CalculateTimeUntilStart()
        {
            var now = DateTime.Now;
            var orderTime = OrderDate;

            if (orderTime <= now || !IsActive)
                return string.Empty;

            var timeLeft = orderTime - now;

            if (timeLeft.TotalHours >= 24)
            {
                return $"Через {timeLeft.Days} дн. {timeLeft.Hours} ч.";
            }
            else if (timeLeft.TotalHours >= 1)
            {
                return $"Через {timeLeft.Hours} ч. {timeLeft.Minutes} мин.";
            }
            else if (timeLeft.TotalMinutes >= 1)
            {
                return $"Через {timeLeft.Minutes} мин.";
            }
            else
            {
                return "Скоро начинается";
            }
        }

        private bool ShouldShowTimer()
        {
            return IsActive && OrderDate > DateTime.Now;
        }

        private bool CanOrderBeCancelled()
        {
            var cancellableStatuses = new[] { "создан", "ожидает оплаты" };
            return cancellableStatuses.Contains(Status) && OrderDate > DateTime.Now;
        }

        private bool CanSetReminderForOrder()
        {
            var activeStatuses = new[] { "создан", "ожидает оплаты", "оплачен", "подтвержден" };
            return activeStatuses.Contains(Status) && OrderDate > DateTime.Now;
        }

        private bool CanOrderBeDeleted()
        {
            var deletableStatuses = new[] { "завершен", "отменен" };
            return deletableStatuses.Contains(Status);
        }

        private bool IsOrderActive()
        {
            var inactiveStatuses = new[] { "отменен", "завершен" };
            return !inactiveStatuses.Contains(Status);
        }

        // Метод для автоматического завершения заказа по истечении времени
        public bool ShouldBeCompleted()
        {
            if (!IsActive) return false;

            var now = DateTime.Now;
            var orderEndTime = OrderDate.AddHours(2); // Предполагаем среднюю длительность 2 часа

            return orderEndTime <= now;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}