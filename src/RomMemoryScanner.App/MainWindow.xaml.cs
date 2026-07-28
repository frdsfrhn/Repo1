using System.Windows;
using RomMemoryScanner.App.ViewModels;

namespace RomMemoryScanner.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
