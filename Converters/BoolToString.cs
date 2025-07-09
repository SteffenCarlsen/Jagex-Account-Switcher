#region

using System;
using System.Globalization;
using Avalonia.Data.Converters;

#endregion

namespace JagexAccountSwitcher.Converters;

public class BoolToString : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;

        if (parameter is string paramString && paramString.Contains("|"))
        {
            var parts = paramString.Split('|');
            return boolValue ? parts[0] : parts[1];
        }

        return boolValue ? "True" : "False";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

// Static accessor for XAML
public static class StringConverters
{
    public static readonly BoolToString BoolToString = new();
}