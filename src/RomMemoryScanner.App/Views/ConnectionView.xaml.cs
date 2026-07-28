using System.Windows.Controls;
using Microsoft.Win32;
using RomMemoryScanner.App.ViewModels;

namespace RomMemoryScanner.App.Views;

public partial class ConnectionView : UserControl
{
    public ConnectionView()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select a ROM or disc image",
            Filter = "ROM/disc images (*.sfc;*.smc;*.md;*.gen;*.cue;*.iso;*.bin)|*.sfc;*.smc;*.md;*.gen;*.cue;*.iso;*.bin|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true && DataContext is ConnectionViewModel viewModel)
        {
            viewModel.RomPath = dialog.FileName;
            if (viewModel.IdentifyRomCommand.CanExecute(null))
            {
                viewModel.IdentifyRomCommand.Execute(null);
            }
        }
    }
}
