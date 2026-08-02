using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.Converters
{
    public class PriorityToBackgroundConverter : IValueConverter
    {
        private static readonly SolidColorBrush Low = Make("#E8F5E9");
        private static readonly SolidColorBrush Medium = Make("#FFF3E0");
        private static readonly SolidColorBrush High = Make("#FFEBEE");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Priority p)
            {
                return p switch
                {
                    Priority.Low => Low,
                    Priority.Medium => Medium,
                    Priority.High => High,
                    _ => Medium
                };
            }
            return Medium;
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
