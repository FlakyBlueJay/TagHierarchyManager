using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace TagHierarchyManager.UI.Views;

public class TruncationConverter : IValueConverter
{
    public static readonly TruncationConverter Instance = new();

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s) return value;
        var limit = parameter is string p && int.TryParse(p, out var l) ? l : 50;
        return s.Length <= limit ? s : s[..limit] + "...";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}