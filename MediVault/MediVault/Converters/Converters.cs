using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MediVault.Models;

namespace MediVault.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public bool Collapse { get; set; } = true;

    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var b = value is bool v && v;
        if (parameter is string s && s == "invert") b = !b;
        if (Invert) b = !b;
        return b ? Visibility.Visible : (Collapse ? Visibility.Collapsed : Visibility.Hidden);
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility vis && vis == Visibility.Visible;
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var hasValue = value != null;
        if (parameter is string s && s == "invert") hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

public class StringNullOrEmptyToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string;
        var hasValue = !string.IsNullOrWhiteSpace(s);
        if (parameter is string p && p == "invert") hasValue = !hasValue;
        return hasValue ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value != null && parameter != null && value.ToString() == parameter.ToString();

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null && targetType.IsEnum)
            return Enum.Parse(targetType, parameter.ToString()!);
        return Binding.DoNothing;
    }
}

public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AppointmentStatus s)
        {
            return s switch
            {
                AppointmentStatus.Scheduled => new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB)),
                AppointmentStatus.Completed => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),
                AppointmentStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
                AppointmentStatus.NoShow => new SolidColorBrush(Color.FromRgb(0xCA, 0x8A, 0x04)),
                _ => Brushes.Gray
            };
        }
        if (value is AuditAction a)
        {
            return a switch
            {
                AuditAction.Create => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),
                AuditAction.Update => new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB)),
                AuditAction.Delete => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
                AuditAction.Login => new SolidColorBrush(Color.FromRgb(0x6D, 0x28, 0xD9)),
                AuditAction.Logout => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                _ => Brushes.Gray
            };
        }
        if (value is ConditionSeverity sev)
        {
            return sev switch
            {
                ConditionSeverity.Mild => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),
                ConditionSeverity.Moderate => new SolidColorBrush(Color.FromRgb(0xCA, 0x8A, 0x04)),
                ConditionSeverity.Severe => new SolidColorBrush(Color.FromRgb(0xEA, 0x58, 0x0C)),
                ConditionSeverity.Critical => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),
                _ => Brushes.Gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public class InitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        var s = value as string ?? string.Empty;
        if (string.IsNullOrWhiteSpace(s)) return "?";
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
        return ($"{parts[0][0]}{parts[^1][0]}").ToUpper();
    }
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public class GenderToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Gender g)
        {
            return g switch
            {
                Gender.Male => new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0xEB)),
                Gender.Female => new SolidColorBrush(Color.FromRgb(0xDB, 0x27, 0x77)),
                _ => new SolidColorBrush(Color.FromRgb(0x6D, 0x28, 0xD9))
            };
        }
        return Brushes.Gray;
    }
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public class CountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is int i && i > 0;
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public class ActiveToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b
            ? new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A))   // green
            : new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));  // slate
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

public class ActiveToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "Active" : "Inactive";
    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}
