using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CaroGame.Wpf.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = false;

        if (value is bool b)
        {
            isVisible = b;
        }
        else if (value is int i)
        {
            isVisible = i > 0;
        }
        else if (value is string s)
        {
            isVisible = !string.IsNullOrWhiteSpace(s);
        }
        else if (value is ICollection coll)
        {
            isVisible = coll.Count > 0;
        }
        else if (value != null)
        {
            isVisible = true;
        }

        if (Invert)
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
