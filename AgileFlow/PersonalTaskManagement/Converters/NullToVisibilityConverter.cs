using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PersonalTaskManagement.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null || (value is string s && string.IsNullOrEmpty(s));
            bool show = Invert ? isNull : !isNull;
            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
