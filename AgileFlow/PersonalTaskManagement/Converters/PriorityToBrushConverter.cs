using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PersonalTaskManagement.Models;

namespace PersonalTaskManagement.Converters
{
    public class PriorityToBrushConverter : IValueConverter
    {
        public static readonly SolidColorBrush LowBrush = MakeBrush("#43A047");
        public static readonly SolidColorBrush MediumBrush = MakeBrush("#FB8C00");
        public static readonly SolidColorBrush HighBrush = MakeBrush("#E53935");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Priority p)
            {
                return p switch
                {
                    Priority.Low => LowBrush,
                    Priority.Medium => MediumBrush,
                    Priority.High => HighBrush,
                    _ => MediumBrush
                };
            }
            return MediumBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static SolidColorBrush MakeBrush(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
