using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScreenHopper.Converters;

public sealed class ZoneSelectedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        return Equals(value, parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is not null)
        {
            return parameter;
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
