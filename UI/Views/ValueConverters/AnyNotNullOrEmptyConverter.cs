using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;

namespace TagHierarchyManager.UI.Views;

public class AnyNotNullOrEmptyMultiConverter : IMultiValueConverter
{
    public static readonly AnyNotNullOrEmptyMultiConverter Instance = new();
    
    public object Convert(IList<object?> values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.Any(v => v is string s ? !string.IsNullOrEmpty(s) : v is not null);
    }
}