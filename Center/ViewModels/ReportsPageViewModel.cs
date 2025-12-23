using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BLL;
using BLL.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Collections.Generic;

namespace Center.ViewModels
{
    public class ReportsPageViewModel : INotifyPropertyChanged
    {
        private readonly ReportService _reportService;

        #region Properties

        private string _selectedTab = "dashboard";
        public string SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged();
                    UpdateTabVisibility();
                    OnPropertyChanged(nameof(IsDashboardTab));
                    OnPropertyChanged(nameof(IsRevenueTab));
                    LoadTabData();
                }
            }
        }

        // Tab visibility
        private bool _isDashboardVisible = true;
        public bool IsDashboardVisible
        {
            get => _isDashboardVisible;
            set => SetField(ref _isDashboardVisible, value);
        }

        private bool _isRevenueVisible = false;
        public bool IsRevenueVisible
        {
            get => _isRevenueVisible;
            set => SetField(ref _isRevenueVisible, value);
        }

        // Tab selection
        public bool IsDashboardTab => SelectedTab == "dashboard";
        public bool IsRevenueTab => SelectedTab == "revenue";

        // Visibility properties
        public bool HasTopServices => RevenueReport?.TopServices != null && RevenueReport.TopServices.Count > 0;
        public bool HasNoTopServices => RevenueReport?.TopServices == null || RevenueReport.TopServices.Count == 0;
        public bool HasCustomerStatistics => RevenueReport?.CustomerStatistics != null && RevenueReport.CustomerStatistics.Count > 0;
        public bool HasNoCustomerStatistics => RevenueReport?.CustomerStatistics == null || RevenueReport.CustomerStatistics.Count == 0;

        // Dashboard
        private DashboardMetrics _dashboardMetrics = new DashboardMetrics();
        public DashboardMetrics DashboardMetrics
        {
            get => _dashboardMetrics;
            set
            {
                SetField(ref _dashboardMetrics, value);
                OnPropertyChanged(nameof(TotalRevenueFormatted));
                OnPropertyChanged(nameof(MostPopularServiceRevenueFormatted));
                OnPropertyChanged(nameof(MostPopularServiceInfo));
                OnPropertyChanged(nameof(MostPopularMonthInfo));
            }
        }

        // Revenue report
        private RevenueReport _revenueReport = new RevenueReport();
        public RevenueReport RevenueReport
        {
            get => _revenueReport;
            set
            {
                SetField(ref _revenueReport, value);
                if (_revenueReport.TopServices == null)
                    _revenueReport.TopServices = new List<ServiceRevenue>();
                if (_revenueReport.CustomerStatistics == null)
                    _revenueReport.CustomerStatistics = new List<CustomerStats>();
                
                OnPropertyChanged(nameof(HasTopServices));
                OnPropertyChanged(nameof(HasNoTopServices));
                OnPropertyChanged(nameof(HasCustomerStatistics));
                OnPropertyChanged(nameof(HasNoCustomerStatistics));
            }
        }

        // Date filters
        private DateTime _revenueStartDate = DateTime.Now.AddDays(-30);
        public DateTime RevenueStartDate
        {
            get => _revenueStartDate;
            set => SetField(ref _revenueStartDate, value);
        }

        private DateTime _revenueEndDate = DateTime.Now;
        public DateTime RevenueEndDate
        {
            get => _revenueEndDate;
            set => SetField(ref _revenueEndDate, value);
        }

        // Formatted properties
        public string TotalRevenueFormatted => $"{DashboardMetrics?.TotalRevenue:N0} ₽";
        public string MostPopularServiceRevenueFormatted => $"{DashboardMetrics?.MostPopularServiceRevenue:N0} ₽";
        public string MostPopularServiceInfo => DashboardMetrics != null && !string.IsNullOrEmpty(DashboardMetrics.MostPopularService) ?
            $"{DashboardMetrics.MostPopularService}\n(заказана {DashboardMetrics.MostPopularServiceCount} раз)" : "Нет данных";
        public string MostPopularMonthInfo => DashboardMetrics != null && !string.IsNullOrEmpty(DashboardMetrics.MostPopularMonth) ?
            $"{DashboardMetrics.MostPopularMonth}\n({DashboardMetrics.MostPopularMonthOrders} заказов)" : "Нет данных";

        #endregion

        #region Commands

        public ICommand SelectTabCommand { get; }
        public ICommand LoadDashboardCommand { get; }
        public ICommand LoadRevenueReportCommand { get; }

        #endregion

        #region Constructor

        public ReportsPageViewModel()
        {
            _reportService = App.ServiceProvider.GetRequiredService<ReportService>();

            SelectTabCommand = new EntertainmentCenter.Commands.RelayCommand<string>(SelectTab);
            LoadDashboardCommand = new EntertainmentCenter.Commands.RelayCommand(() => _ = LoadDashboardAsync());
            LoadRevenueReportCommand = new EntertainmentCenter.Commands.RelayCommand(() => _ = LoadRevenueReportAsync());

            UpdateTabVisibility();
            _ = LoadDashboardAsync();
        }

        #endregion

        #region Methods

        private void SelectTab(string tab)
        {
            SelectedTab = tab;
        }

        private void LoadTabData()
        {
            switch (SelectedTab)
            {
                case "dashboard":
                    _ = LoadDashboardAsync();
                    break;
                case "revenue":
                    _ = LoadRevenueReportAsync();
                    break;
            }
        }

        private async Task LoadDashboardAsync()
        {
            try
            {
                var metrics = await _reportService.GetDashboardMetricsAsync();
                DashboardMetrics = metrics ?? new DashboardMetrics();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки дашборда: {ex.Message}");
                DashboardMetrics = new DashboardMetrics();
            }
        }

        private async Task LoadRevenueReportAsync()
        {
            try
            {
                var report = await _reportService.GetRevenueReportAsync(RevenueStartDate, RevenueEndDate);
                RevenueReport = report ?? new RevenueReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки отчета о выручке: {ex.Message}");
                RevenueReport = new RevenueReport();
            }
        }

        private void UpdateTabVisibility()
        {
            IsDashboardVisible = SelectedTab == "dashboard";
            IsRevenueVisible = SelectedTab == "revenue";
        }

        #endregion

        #region INotifyPropertyChanged

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

        #endregion
    }
}
