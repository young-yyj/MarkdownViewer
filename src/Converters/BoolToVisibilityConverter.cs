using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarkdownViewer.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value switch
        {
            bool b => b,
            int n => n > 0,
            _ => false
        };
        if (Invert) boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible ? !Invert : Invert;
    }
}
