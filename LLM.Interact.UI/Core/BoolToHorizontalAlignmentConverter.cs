using Avalonia.Data.Converters;
using Avalonia.Layout;
using System;
using System.Globalization;

namespace LLM.Interact.UI.Core
{
    public class BoolToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
