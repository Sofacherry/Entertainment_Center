using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Center.Converters
{
    public class ActiveStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
            {
                // Возвращаем активный стиль
                return Application.Current.FindResource("ActiveMenuButtonStyle");
            }

            // Возвращаем обычный стиль
            return Application.Current.FindResource("MenuButtonStyle");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}