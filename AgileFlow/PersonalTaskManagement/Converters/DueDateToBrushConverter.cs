using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PersonalTaskManagement.Converters
{
    public class DueDateToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Overdue = Make("#C62828");
        private static readonly SolidColorBrush Today = Make("#EF6C00");
        private static readonly SolidColorBrush Soon = Make("#F9A825");
        private static readonly SolidColorBrush Normal = Make("#546E7A");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
            {
                var days = (dt.Date - DateTime.Today).TotalDays;
                if (days < 0) return Overdue;
                if (days < 1) return Today;
                if (days <= 3) return Soon;
                return Normal;
            }
            if (value is DateTime?)
            {
                var nullable = (DateTime?)value;
                if (!nullable.HasValue) return Normal;
                return Convert(nullable.Value, targetType, parameter, culture);
            }
            return Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush Make(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
