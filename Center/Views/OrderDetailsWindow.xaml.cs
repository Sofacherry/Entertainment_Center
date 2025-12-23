using System.Windows;
using Center.ViewModels;

namespace Center.Views
{
    public partial class OrderDetailsWindow : Window
    {
        public OrderDetailsWindow(OrderViewModel order)
        {
            InitializeComponent();
            DataContext = new OrderDetailsViewModel(this, order);
        }
    }
}