using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DAL;
using Microsoft.EntityFrameworkCore;
using Center.Views;
using Center.ViewModels;
using BLL;
using Microsoft.Extensions.Configuration;

namespace Center
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }
        public static IConfiguration Configuration { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {

            
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            //var viewModel = ServiceProvider.GetRequiredService<CatalogPageViewModel>();
            //var catalogPage = new CatalogPageView();
            //catalogPage.DataContext = viewModel;
            //catalogPage.Show();
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<CatalogService>();
            services.AddScoped<BookingService>();
            services.AddScoped<UserService>();
            services.AddScoped<ReportService>();
            services.AddScoped<AdminCRUDService>();

            services.AddTransient<CatalogPageViewModel>();
            services.AddTransient<MainViewModel>();

            services.AddTransient<MainWindow>();
            services.AddTransient<CatalogPageView>();
        }
    }
}