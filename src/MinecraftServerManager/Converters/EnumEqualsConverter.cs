using System.Globalization;
using Avalonia.Data.Converters;

namespace MinecraftServerManager.Converters;

public sealed class EnumEqualsConverter : IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null)
            return false;
        return value.Equals(parameter);
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
