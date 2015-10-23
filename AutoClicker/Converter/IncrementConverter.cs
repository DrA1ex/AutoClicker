using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoClicker.Converter
{
    public class IncrementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var x = (int)value;
            return x + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}