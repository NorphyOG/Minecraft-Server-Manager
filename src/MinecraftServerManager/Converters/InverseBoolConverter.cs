using System.Globalization;
using Avalonia.Data.Converters;

namespace MinecraftServerManager.Converters;

public sealed class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b ? !b : false;
    }
}
