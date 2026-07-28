using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.App.Views;

/// <summary>Shows the PS1-only write-width picker in <see cref="CheatCodeView"/> only when PS1 is selected.</summary>
public sealed class ConsoleToPs1VisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ConsoleType.Ps1 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
