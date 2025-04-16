using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace JagexAccountSwitcher.Converters
{
    public class BoolToSymbolConverter : IValueConverter
    {
        public string TrueValue { get; set; } = "✓";
        public string FalseValue { get; set; } = "✗";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
                return boolVal ? TrueValue : FalseValue;
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public IBrush TrueValue { get; set; } = Brushes.Green;
        public IBrush FalseValue { get; set; } = Brushes.Red;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolVal)
                return boolVal ? TrueValue : FalseValue;
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}