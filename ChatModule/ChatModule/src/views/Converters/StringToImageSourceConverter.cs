using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace ChatModule.src.views.Converters
{
    public class StringToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is not string raw || string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var input = raw.Trim();

            try
            {
                if (File.Exists(input))
                {
                    return new BitmapImage(new Uri(input, UriKind.Absolute));
                }

                if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
                {
                    return new BitmapImage(uri);
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
