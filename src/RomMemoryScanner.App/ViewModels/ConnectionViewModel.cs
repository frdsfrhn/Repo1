using System.Diagnostics;
using RomMemoryScanner.Core.Database;
using RomMemoryScanner.Core.Identification;
using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.RetroArch;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>Game/process picker (FR-7.1 view 1): connect to RetroArch, load a ROM, and identify/match it against the database.</summary>
public sealed class ConnectionViewModel : ViewModelBase
{
    private readonly GameDatabaseService _databaseService;

    private string _host = "127.0.0.1";
    private int _port = RetroArchClient.DefaultPort;
    private bool _isConnected;
    private string? _retroArchVersion;
    private string _statusMessage = "Not connected.";
    private string? _romPath;
    private ConsoleType _detectedConsole = ConsoleType.Unknown;
    private ConsoleType _selectedConsole = ConsoleType.Snes;
    private RomChecksum? _romChecksum;
    private Game? _matchedGame;
    private bool _retroArchProcessDetected;

    public ConnectionViewModel(GameDatabaseService databaseService)
    {
        _databaseService = databaseService;
        ConnectCommand = new AsyncRelayCommand(ConnectAsync);
        RefreshProcessListCommand = new RelayCommand(RefreshProcessList);
        IdentifyRomCommand = new RelayCommand(_ => IdentifyRom(), _ => !string.IsNullOrWhiteSpace(RomPath));
        RefreshProcessList(null);
    }

    public event EventHandler<Game?>? GameMatched;

    /// <summary>
    /// Fires after every connect attempt (success or failure) so subscribers can re-sync their
    /// held <see cref="Client"/> reference. Deliberately not driven off <see cref="IsConnected"/>'s
    /// PropertyChanged: that only fires when the boolean value changes, but <see cref="Client"/>
    /// itself is replaced (old one disposed) on every call to Connect, including reconnects where
    /// IsConnected stays true — relying on the boolean would leave subscribers holding a disposed client.
    /// </summary>
    public event EventHandler? ConnectionChanged;

    public RetroArchClient? Client { get; private set; }

    public string Host { get => _host; set => SetField(ref _host, value); }
    public int Port { get => _port; set => SetField(ref _port, value); }
    public bool IsConnected { get => _isConnected; private set => SetField(ref _isConnected, value); }
    public string? RetroArchVersion { get => _retroArchVersion; private set => SetField(ref _retroArchVersion, value); }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public bool RetroArchProcessDetected { get => _retroArchProcessDetected; private set => SetField(ref _retroArchProcessDetected, value); }

    public string? RomPath { get => _romPath; set => SetField(ref _romPath, value); }

    public ConsoleType DetectedConsole { get => _detectedConsole; private set => SetField(ref _detectedConsole, value); }

    /// <summary>User override when auto-ID fails or picks the wrong console (FR-1.3).</summary>
    public ConsoleType SelectedConsole
    {
        get => _selectedConsole;
        set
        {
            if (SetField(ref _selectedConsole, value))
            {
                TryMatchDatabase();
            }
        }
    }

    public IEnumerable<ConsoleType> AvailableConsoles => new[] { ConsoleType.Snes, ConsoleType.Genesis, ConsoleType.Ps1 };

    public RomChecksum? RomChecksum { get => _romChecksum; private set => SetField(ref _romChecksum, value); }

    public Game? MatchedGame
    {
        get => _matchedGame;
        private set
        {
            if (SetField(ref _matchedGame, value))
            {
                GameMatched?.Invoke(this, value);
            }
        }
    }

    public AsyncRelayCommand ConnectCommand { get; }
    public RelayCommand RefreshProcessListCommand { get; }
    public RelayCommand IdentifyRomCommand { get; }

    private async Task ConnectAsync()
    {
        StatusMessage = $"Connecting to RetroArch at {Host}:{Port}...";
        Client?.Dispose();
        Client = new RetroArchClient(Host, Port);

        try
        {
            string? version = await Client.GetVersionAsync().ConfigureAwait(true);
            if (version is null)
            {
                IsConnected = false;
                StatusMessage = "No response from RetroArch. Enable Settings > Network > Network Commands, and confirm the port.";
                return;
            }

            RetroArchVersion = version;
            IsConnected = true;
            StatusMessage = BuildStatusMessage();
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Connection failed: {ex.Message}";
        }
        finally
        {
            ConnectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RefreshProcessList(object? _)
    {
        // FR-2.1: detect running supported emulator processes. Phase 1 only needs this as a
        // friendly hint (RetroArch is always reached over UDP, regardless of process visibility);
        // per-process memory attachment is Phase 3 (§9).
        RetroArchProcessDetected = Process.GetProcessesByName("retroarch").Length > 0
            || Process.GetProcessesByName("RetroArch").Length > 0;
    }

    private void IdentifyRom()
    {
        if (string.IsNullOrWhiteSpace(RomPath))
        {
            return;
        }

        try
        {
            IdentifiedRom identified = RomIdentifier.Identify(RomPath);
            DetectedConsole = identified.Console;
            RomChecksum = identified.Checksum;

            if (identified.RequiresManualConsoleSelection)
            {
                StatusMessage = "Could not detect console from file extension — pick one manually.";
            }
            else
            {
                // Set directly rather than through the SelectedConsole property: its setter only
                // re-matches the database when the value actually *changes*, which would silently
                // skip re-matching when re-identifying a ROM for the same console as before.
                _selectedConsole = identified.Console;
                OnPropertyChanged(nameof(SelectedConsole));
            }

            TryMatchDatabase();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not read ROM: {ex.Message}";
        }
    }

    private void TryMatchDatabase()
    {
        if (RomChecksum is null)
        {
            return;
        }

        IReadOnlyList<Game> games = _databaseService.LoadAll();
        MatchedGame = GameDatabaseService.FindByChecksum(games, SelectedConsole, RomChecksum);
        StatusMessage = MatchedGame is not null
            ? BuildStatusMessage()
            : $"No database entry matches this ROM's checksum for {SelectedConsole}. You can still tag values manually via live-scan.";
    }

    private string BuildStatusMessage()
    {
        // FR-7.3: "Attached to [emulator] — [game title] — [live/static mode]."
        string emulator = IsConnected ? "RetroArch" : "(not connected)";
        string game = MatchedGame?.Title ?? "(no game identified)";
        string mode = IsConnected ? "live" : "static";
        return $"Attached to {emulator} — {game} — {mode} mode";
    }
}
