using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace ChatModule.src.views.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            var isVisible = value != null;
            if (invert)
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
