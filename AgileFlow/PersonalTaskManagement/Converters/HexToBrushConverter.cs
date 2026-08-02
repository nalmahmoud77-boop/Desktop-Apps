using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PersonalTaskManagement.Converters
{
    public class HexToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush Fallback;

        static HexToBrushConverter()
        {
            Fallback = new SolidColorBrush(Color.FromRgb(0x60, 0x7D, 0x8B));
            Fallback.Freeze();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
                    brush.Freeze();
                    return brush;
                }
                catch { return Fallback; }
            }
            return Fallback;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
