using System.Globalization;

namespace TutorLocal;

/// <summary>Utile pour convertir une valeur booléenne en opposition (UI de chat, visibilité...).</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value ?? false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : value ?? false;
}

/// <summary>Convertisseur booléen-compilé vers une chaîne sélectionnable par le XAML.</summary>
public class BoolToStringConverter : IValueConverter
{
    public string? TrueValue { get; set; }

    public string? FalseValue { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? TrueValue ?? string.Empty : FalseValue ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Rend un élément visible seulement si la ressource est renseignée.</summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int count && count > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
