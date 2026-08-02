using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PersonalTaskManagement.Converters
{
    /// <summary>
    /// Maps a string (e.g. a column name) to a stable colour from a fixed palette,
    /// so each column gets a consistent accent without needing a stored colour.
    /// </summary>
    public class StringToAccentBrushConverter : IValueConverter
    {
        private static readonly string[] Palette =
        {
            "#2563EB", // blue
            "#7C3AED", // violet
            "#059669", // green
            "#D97706", // amber
            "#DB2777", // pink
            "#0891B2", // cyan
            "#4F46E5", // indigo
            "#DC2626"  // red
        };

        private static readonly SolidColorBrush[] Brushes;

        static StringToAccentBrushConverter()
        {
            Brushes = new SolidColorBrush[Palette.Length];
            for (int i = 0; i < Palette.Length; i++)
            {
                var b = (SolidColorBrush)new BrushConverter().ConvertFromString(Palette[i])!;
                b.Freeze();
                Brushes[i] = b;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = value as string ?? string.Empty;
            int hash = 0;
            foreach (char c in s) hash = (hash * 31 + c) & 0x7FFFFFFF;
            return Brushes[hash % Brushes.Length];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
