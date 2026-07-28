using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RomMemoryScanner.App.ViewModels;

namespace RomMemoryScanner.App.Views;

public partial class DatabaseBrowserView : UserControl
{
    public DatabaseBrowserView()
    {
        InitializeComponent();
    }

    private DatabaseBrowserViewModel? ViewModel => DataContext as DatabaseBrowserViewModel;

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Import a game address map", Filter = "JSON files (*.json)|*.json" };
        if (dialog.ShowDialog() == true)
        {
            ViewModel?.Import(dialog.FileName);
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedGame is not { } game)
        {
            MessageBox.Show("Select a game first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog { Title = "Export game address map", Filter = "JSON files (*.json)|*.json", FileName = game.Title + ".json" };
        if (dialog.ShowDialog() == true)
        {
            ViewModel?.Export(game, dialog.FileName);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel?.SelectedGame is not { } game)
        {
            MessageBox.Show("Select a game first.", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ViewModel.SaveToLocalDatabase(game);
    }
}
