using System.Collections.ObjectModel;
using System.Windows.Input;
using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Numeric;
using RomMemoryScanner.Core.RetroArch;
using RomMemoryScanner.Core.Scanning;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>
/// Live value-scan workspace (FR-7.1 view 2, FR-2.4): find an address you don't know yet by
/// telling the app a value you *do* know (what the game shows on screen), then narrowing across
/// passes as that value changes in-game. This is how you discover HP/MP/Gold addresses for a game
/// with no database entry — the Address Database and Cheat Codes tabs need an address up front;
/// this tab is how you find one in the first place.
/// </summary>
public sealed class LiveScanViewModel : ViewModelBase
{
    private RetroArchClient? _client;
    private ConsoleType _console;
    private MemoryScanner? _scanner;

    private DataType _dataType = DataType.U8;
    private ByteOrder _byteOrder = ByteOrder.LittleEndian;
    private string _knownValueText = "";
    private string _newValueText = "";
    private ScanComparison _comparison = ScanComparison.Decreased;

    private bool _isConnected;
    private bool _hasActiveScan;
    private int _candidateCount;
    private string? _statusMessage = "Connect to RetroArch on the Game/Process tab first.";
    private string _tagLabel = "";
    private ScanCandidate? _selectedCandidate;

    public LiveScanViewModel()
    {
        StartScanCommand = new AsyncRelayCommand(StartScanAsync, () => _client is not null && !string.IsNullOrWhiteSpace(KnownValueText));
        StartUnknownScanCommand = new AsyncRelayCommand(StartUnknownScanAsync, () => _client is not null);
        NextScanCommand = new AsyncRelayCommand(NextScanAsync, () => HasActiveScan);
        ResetCommand = new RelayCommand(_ => Reset(), _ => HasActiveScan);
        TagSelectedCommand = new RelayCommand(_ => TagSelected(), _ => SelectedCandidate is not null && !string.IsNullOrWhiteSpace(TagLabel));
    }

    /// <summary>Raised when the user tags a candidate; MainViewModel wires this into the Dashboard (FR-2.5).</summary>
    public event EventHandler<TrackedValue>? ValueTagged;

    public IEnumerable<DataType> AvailableDataTypes => new[]
    {
        DataType.U8, DataType.U16, DataType.U32, DataType.Bcd8, DataType.Bcd16, DataType.Bcd32,
    };

    public IEnumerable<ByteOrder> AvailableByteOrders => new[] { ByteOrder.LittleEndian, ByteOrder.BigEndian };

    public IEnumerable<ScanComparison> AvailableComparisons => new[]
    {
        ScanComparison.Decreased, ScanComparison.Increased, ScanComparison.Changed, ScanComparison.Unchanged, ScanComparison.EqualTo,
    };

    public DataType DataType { get => _dataType; set => SetField(ref _dataType, value); }
    public ByteOrder ByteOrder { get => _byteOrder; set => SetField(ref _byteOrder, value); }
    public string KnownValueText { get => _knownValueText; set => SetField(ref _knownValueText, value); }
    public string NewValueText { get => _newValueText; set => SetField(ref _newValueText, value); }
    public ScanComparison Comparison { get => _comparison; set => SetField(ref _comparison, value); }

    public bool IsConnected { get => _isConnected; private set => SetField(ref _isConnected, value); }
    public bool HasActiveScan { get => _hasActiveScan; private set => SetField(ref _hasActiveScan, value); }
    public int CandidateCount { get => _candidateCount; private set => SetField(ref _candidateCount, value); }
    public string? StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }

    public string TagLabel { get => _tagLabel; set => SetField(ref _tagLabel, value); }
    public ScanCandidate? SelectedCandidate { get => _selectedCandidate; set => SetField(ref _selectedCandidate, value); }

    public ObservableCollection<ScanCandidate> Candidates { get; } = new();

    public AsyncRelayCommand StartScanCommand { get; }
    public AsyncRelayCommand StartUnknownScanCommand { get; }
    public AsyncRelayCommand NextScanCommand { get; }
    public RelayCommand ResetCommand { get; }
    public RelayCommand TagSelectedCommand { get; }

    public void AttachClient(RetroArchClient? client, ConsoleType console)
    {
        _client = client;
        _console = console;
        IsConnected = client is not null;
        Reset();
        StatusMessage = IsConnected
            ? $"Ready to scan {console} WRAM."
            : "Connect to RetroArch on the Game/Process tab first.";
    }

    private async Task StartScanAsync()
    {
        if (_client is null)
        {
            return;
        }

        if (!TryConvertToRaw(KnownValueText, out uint rawValue, out string? error))
        {
            StatusMessage = error;
            return;
        }

        try
        {
            _scanner = new MemoryScanner(new RetroArchSnapshotReader(_client, _console), _console);
            StatusMessage = "Scanning...";
            int count = await _scanner.InitialScanAsync(rawValue, DataType, ByteOrder).ConfigureAwait(true);
            RefreshCandidates();
            StatusMessage = $"Initial scan found {count} candidate(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
    }

    /// <summary>
    /// "Unknown initial value" scan (FR-2.4 variant): for things with no readable number on
    /// screen — item IDs, flags — snapshots everything instead of filtering by a known value.
    /// Change the thing in-game, then use Next scan with "Changed" to start narrowing.
    /// </summary>
    private async Task StartUnknownScanAsync()
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            _scanner = new MemoryScanner(new RetroArchSnapshotReader(_client, _console), _console);
            StatusMessage = "Scanning...";
            int count = await _scanner.InitialScanUnknownAsync(DataType, ByteOrder).ConfigureAwait(true);
            RefreshCandidates();
            Comparison = ScanComparison.Changed;
            StatusMessage = $"Snapshotted {count} offset(s). Change the item/value in-game, then Next scan with \"Changed.\"";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
    }

    private async Task NextScanAsync()
    {
        if (_scanner is null)
        {
            return;
        }

        uint? exactValue = null;
        if (Comparison == ScanComparison.EqualTo)
        {
            if (!TryConvertToRaw(NewValueText, out uint rawValue, out string? error))
            {
                StatusMessage = error;
                return;
            }

            exactValue = rawValue;
        }

        try
        {
            StatusMessage = "Scanning...";
            int count = await _scanner.NextScanAsync(Comparison, exactValue).ConfigureAwait(true);
            RefreshCandidates();
            StatusMessage = $"Narrowed to {count} candidate(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
    }

    private void TagSelected()
    {
        if (SelectedCandidate is null || string.IsNullOrWhiteSpace(TagLabel))
        {
            return;
        }

        var trackedValue = new TrackedValue
        {
            Label = TagLabel,
            ConsoleAddress = SelectedCandidate.ConsoleAddress,
            DataType = DataType,
            ByteOrder = ByteOrder,
            Notes = "Found via live value-scan (FR-2.5).",
        };

        ValueTagged?.Invoke(this, trackedValue);
        StatusMessage = $"Tagged '{TagLabel}' at 0x{SelectedCandidate.ConsoleAddress:X6} — now visible on the Dashboard tab.";
        TagLabel = "";
    }

    private void Reset()
    {
        _scanner?.Reset();
        _scanner = null;
        HasActiveScan = false;
        CandidateCount = 0;
        Candidates.Clear();
        SelectedCandidate = null;
        CommandManager.InvalidateRequerySuggested();
    }

    private void RefreshCandidates()
    {
        HasActiveScan = _scanner is not null && _scanner.HasActiveScan;
        CandidateCount = _scanner?.CandidateCount ?? 0;

        Candidates.Clear();
        if (_scanner is not null)
        {
            foreach (ScanCandidate candidate in _scanner.GetCandidates())
            {
                Candidates.Add(candidate);
            }
        }

        // Reset/NextScan's enabled state depends on HasActiveScan, which just changed as a side
        // effect of an async scan completing rather than direct user input — WPF's automatic
        // command requery doesn't reliably catch that pattern, so it's forced explicitly here
        // rather than leaving Reset stuck looking disabled after a scan finishes.
        CommandManager.InvalidateRequerySuggested();
    }

    private bool TryConvertToRaw(string text, out uint rawValue, out string? error)
    {
        error = null;
        rawValue = 0;

        if (!uint.TryParse(text.Trim(), out uint decimalValue))
        {
            error = $"'{text}' is not a valid decimal number.";
            return false;
        }

        try
        {
            rawValue = DataType is DataType.Bcd8 or DataType.Bcd16 or DataType.Bcd32
                ? BcdConverter.DecimalToBcd(decimalValue, DataType.ByteWidth())
                : decimalValue;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or ArgumentException)
        {
            error = ex.Message;
            return false;
        }
    }
}
