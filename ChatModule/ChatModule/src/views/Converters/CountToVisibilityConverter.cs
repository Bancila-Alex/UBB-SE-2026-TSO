using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace ChatModule.src.views.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            var count = 0;

            if (value is int intValue)
            {
                count = intValue;
            }
            else if (value is long longValue)
            {
                count = (int)Math.Min(longValue, int.MaxValue);
            }

            var visible = count > 0;
            if (invert)
            {
                visible = !visible;
            }

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
