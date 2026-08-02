using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PersonalTaskManagement.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public bool UseHidden { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            if (Invert) flag = !flag;
            return flag ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = value is Visibility vis && vis == Visibility.Visible;
            return Invert ? !v : v;
        }
    }
}
