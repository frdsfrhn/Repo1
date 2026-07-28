using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.App.Views;

/// <summary>
/// Shows the Game Genie/PAR format picker in <see cref="CheatCodeView"/> only for SNES/Genesis —
/// PS1 only ever had one format (GameShark/PAR), so there's nothing to choose there.
/// </summary>
public sealed class ConsoleToFormatPickerVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is ConsoleType.Snes or ConsoleType.Genesis ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
