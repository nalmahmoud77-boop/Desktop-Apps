using System;
using System.Globalization;
using System.Windows.Data;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.Converters
{
    public class PriorityLabelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                Priority.Low => "Low",
                Priority.Medium => "Medium",
                Priority.High => "High",
                _ => string.Empty
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
