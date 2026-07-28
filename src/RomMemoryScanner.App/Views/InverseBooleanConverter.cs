using System.Globalization;
using System.Windows.Data;

namespace RomMemoryScanner.App.Views;

/// <summary>Used to disable scan-setup controls (data type/byte order/known value) once a scan is active — changing them mid-scan would silently corrupt the candidate list.</summary>
public sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}
