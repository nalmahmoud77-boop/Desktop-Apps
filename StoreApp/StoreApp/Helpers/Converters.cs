using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StoreApp.Enums;

namespace StoreApp.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var b = value is bool bv && bv;
            if (Invert) b = !b;
            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
            return (Invert ? !isEmpty : isEmpty) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class GreaterThanZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i) return i > 0 ? Visibility.Visible : Visibility.Collapsed;
            if (value is decimal d) return d > 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class OrderStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not OrderStatus s) return Brushes.Gray;
            return s switch
            {
                OrderStatus.Pending => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                OrderStatus.Confirmed => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                OrderStatus.Processing => new SolidColorBrush(Color.FromRgb(0x63, 0x66, 0xF1)),
                OrderStatus.Shipped => new SolidColorBrush(Color.FromRgb(0x06, 0xB6, 0xD4)),
                OrderStatus.Delivered => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                OrderStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                OrderStatus.Refunded => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                _ => Brushes.Gray
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class ProductStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ProductStatus s) return Brushes.Gray;
            return s switch
            {
                ProductStatus.Active => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
                ProductStatus.OutOfStock => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
                ProductStatus.Discontinued => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                ProductStatus.Draft => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
                _ => Brushes.Gray
            };
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class StringToImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var s = value as string;
                if (string.IsNullOrWhiteSpace(s)) return null;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(s, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal d) return d.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            if (value is double db) return db.ToString("C", CultureInfo.GetCultureInfo("en-US"));
            return value?.ToString() ?? string.Empty;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
