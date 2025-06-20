using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace LLM.Interact.UI.Core
{
    public class BoolToBubbleColorConverter : IValueConverter
    {
        // 用户消息气泡颜色（浅蓝色）
        private static readonly SolidColorBrush UserBrush = new(Color.Parse("#D4E6FF"));

        // AI消息气泡颜色（浅灰色）
        private static readonly SolidColorBrush AIBrush = new(Color.Parse("#F0F0F0"));

        // 强调色（用于系统消息）
        private static readonly SolidColorBrush AccentBrush = new(Color.Parse("#4A6572"));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUserMessage)
            {
                // 用户消息返回浅蓝色
                return isUserMessage ? UserBrush : AIBrush;
            }

            // 默认返回AI颜色
            return AIBrush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
