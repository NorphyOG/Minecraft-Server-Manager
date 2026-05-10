using System.Globalization;
using Avalonia.Data.Converters;

namespace MinecraftServerManager.Converters;

/// <summary>True wenn der Wert eine nicht-leere Zeichenkette ist.</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, System.Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrWhiteSpace(s);
    }

    public object? ConvertBack(object? value, System.Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
