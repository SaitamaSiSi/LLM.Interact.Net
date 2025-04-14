using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace LLM.Interact.UI.Core
{
    public class BoolToBubbleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Colors.Green : Colors.Yellow;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
