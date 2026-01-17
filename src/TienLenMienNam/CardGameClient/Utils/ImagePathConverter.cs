using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using CardGameClient.Models;

namespace CardGameClient.Utils
{
    public sealed class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Card card = null;

                if (value is Card c) card = c;
                else if (value is string code) card = Card.FromCode(code);

                if (card == null) return null;

                string fileName = card.ToImageFileName();
                string uri = $"pack://application:,,,/Resources/Cards/{fileName}";
                return new BitmapImage(new Uri(uri, UriKind.Absolute));
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
