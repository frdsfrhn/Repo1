using System.IO;
using RomMemoryScanner.Core.Database;
using RomMemoryScanner.Core.RetroArch;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>Composes the five main views (FR-7.1) and wires cross-view state (a matched game flows into the dashboard).</summary>
public sealed class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        string databaseRoot = Path.Combine(AppContext.BaseDirectory, "database");
        var databaseService = new GameDatabaseService(databaseRoot);

        Connection = new ConnectionViewModel(databaseService);
        Dashboard = new DashboardViewModel();
        LiveScan = new LiveScanViewModel();
        CheatCode = new CheatCodeViewModel();
        DatabaseBrowser = new DatabaseBrowserViewModel(databaseService, databaseRoot);

        Connection.GameMatched += (_, game) => Dashboard.LoadFromGame(game);
        Connection.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(ConnectionViewModel.IsConnected) or nameof(ConnectionViewModel.SelectedConsole))
            {
                RetroArchClient? client = Connection.IsConnected ? Connection.Client : null;
                Dashboard.AttachClient(client, Connection.SelectedConsole);
                LiveScan.AttachClient(client, Connection.SelectedConsole);
            }
        };

        // FR-2.5: an address found via live-scan gets tagged and shows up on the Dashboard immediately.
        LiveScan.ValueTagged += (_, trackedValue) => Dashboard.AddTrackedValue(trackedValue);
    }

    public ConnectionViewModel Connection { get; }
    public DashboardViewModel Dashboard { get; }
    public LiveScanViewModel LiveScan { get; }
    public CheatCodeViewModel CheatCode { get; }
    public DatabaseBrowserViewModel DatabaseBrowser { get; }
}
