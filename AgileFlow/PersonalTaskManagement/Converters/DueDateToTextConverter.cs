using System;
using System.Globalization;
using System.Windows.Data;

namespace PersonalTaskManagement.Converters
{
    public class DueDateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime? d = value as DateTime?;
            if (value is DateTime dt) d = dt;
            if (!d.HasValue) return string.Empty;

            var days = (int)Math.Floor((d.Value.Date - DateTime.Today).TotalDays);
            var datePart = d.Value.ToString("MMM d", culture);

            if (days < 0) return $"{datePart} • Overdue";
            if (days == 0) return $"{datePart} • Today";
            if (days == 1) return $"{datePart} • Tomorrow";
            if (days <= 7) return $"{datePart} • In {days}d";
            return datePart;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
