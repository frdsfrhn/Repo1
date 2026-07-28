using System.Collections.ObjectModel;
using System.Windows.Threading;
using RomMemoryScanner.Core.Addressing;
using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Numeric;
using RomMemoryScanner.Core.RetroArch;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>
/// Tagged-values dashboard (FR-7.1 view 3, FR-7.2): the "character sheet" style view — one row
/// per tracked value, hex + decimal side by side, edit + freeze toggle per row (FR-2.6, FR-4.4).
/// Polls RetroArch on a timer to refresh live values and to re-assert frozen ones.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private readonly DispatcherTimer _pollTimer;
    private RetroArchClient? _client;
    private ConsoleType _console;
    private string? _gameTitle;
    private bool _isPolling;

    private string _manualLabel = "";
    private string _manualAddressHex = "";
    private DataType _manualDataType = DataType.U8;
    private ByteOrder _manualByteOrder = ByteOrder.LittleEndian;
    private string? _manualEntryError;

    public DashboardViewModel()
    {
        Rows = new ObservableCollection<TrackedValueRowViewModel>();
        AuditLog = new ObservableCollection<string>();
        _pollTimer = new DispatcherTimer { Interval = PollInterval };
        _pollTimer.Tick += async (_, _) =>
        {
            // This fires on the UI thread via an async-void-shaped event handler: any exception
            // that escapes it is unhandled and crashes the whole app (WPF has no way to catch it
            // from outside). A background poll loop running forever must never be able to do that,
            // regardless of what bug might someday cause it to throw.
            try
            {
                await PollAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                AuditLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Poll error (recovered): {ex.Message}");
                TrimAuditLog();
            }
        };
        AddManualCommand = new RelayCommand(_ => AddManual(), _ => !string.IsNullOrWhiteSpace(ManualLabel) && !string.IsNullOrWhiteSpace(ManualAddressHex));
        RemoveSelectedCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedRow is not null);
    }

    public ObservableCollection<TrackedValueRowViewModel> Rows { get; }

    private TrackedValueRowViewModel? _selectedRow;
    public TrackedValueRowViewModel? SelectedRow { get => _selectedRow; set => SetField(ref _selectedRow, value); }

    public RelayCommand RemoveSelectedCommand { get; }

    private void RemoveSelected()
    {
        if (SelectedRow is null)
        {
            return;
        }

        Rows.Remove(SelectedRow);
        SelectedRow = null;
    }

    /// <summary>NFR-2: any live memory read/write action is logged, visible to the user as an audit trail.</summary>
    public ObservableCollection<string> AuditLog { get; }

    public string? GameTitle
    {
        get => _gameTitle;
        private set => SetField(ref _gameTitle, value);
    }

    /// <summary>Manual entry: know an address already (e.g. from a community source or prior session)? Add it directly, no scan needed.</summary>
    public IEnumerable<DataType> AvailableDataTypes => Enum.GetValues<DataType>();

    public IEnumerable<ByteOrder> AvailableByteOrders => new[] { ByteOrder.LittleEndian, ByteOrder.BigEndian };

    public string ManualLabel { get => _manualLabel; set => SetField(ref _manualLabel, value); }
    public string ManualAddressHex { get => _manualAddressHex; set => SetField(ref _manualAddressHex, value); }
    public DataType ManualDataType { get => _manualDataType; set => SetField(ref _manualDataType, value); }
    public ByteOrder ManualByteOrder { get => _manualByteOrder; set => SetField(ref _manualByteOrder, value); }
    public string? ManualEntryError { get => _manualEntryError; private set => SetField(ref _manualEntryError, value); }

    public RelayCommand AddManualCommand { get; }

    public void AttachClient(RetroArchClient? client, ConsoleType console)
    {
        _client = client;
        _console = console;
        if (client is not null)
        {
            _pollTimer.Start();
        }
        else
        {
            _pollTimer.Stop();
        }
    }

    public void LoadFromGame(Game? game)
    {
        Rows.Clear();
        GameTitle = game?.Title;
        if (game is null)
        {
            return;
        }

        _console = game.Console;
        foreach (TrackedValue trackedValue in game.TrackedValues)
        {
            Rows.Add(new TrackedValueRowViewModel(trackedValue, WriteRowAsync));
        }
    }

    /// <summary>FR-2.5: lets a value found via live-scan be tagged and added here; also usable for manual tagging today.</summary>
    public void AddTrackedValue(TrackedValue trackedValue)
    {
        Rows.Add(new TrackedValueRowViewModel(trackedValue, WriteRowAsync));
    }

    /// <summary>Adds a tracked value from a manually-entered address — no scan needed, for addresses already known from another source.</summary>
    private void AddManual()
    {
        ManualEntryError = null;

        string cleaned = ManualAddressHex.Trim().TrimStart('0', 'x', 'X');
        if (cleaned.Length == 0)
        {
            cleaned = "0";
        }

        if (!uint.TryParse(cleaned, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out uint address))
        {
            ManualEntryError = $"'{ManualAddressHex}' is not a valid hex address.";
            return;
        }

        try
        {
            // Fail fast with a clear message rather than letting the first poll tick surface an
            // AddressOutOfRangeException — Dashboard reads/writes only ever go through this
            // console's WRAM window (see AddressTranslator), so anything outside it can never work.
            AddressTranslator.ToCoreOffset(_console, address);
        }
        catch (AddressOutOfRangeException ex)
        {
            ManualEntryError = ex.Message;
            return;
        }

        var trackedValue = new TrackedValue
        {
            Label = ManualLabel,
            ConsoleAddress = address,
            DataType = ManualDataType,
            ByteOrder = ManualByteOrder,
            Notes = "Added manually.",
        };

        AddTrackedValue(trackedValue);
        ManualLabel = "";
        ManualAddressHex = "";
    }

    private async Task PollAsync()
    {
        if (_client is null)
        {
            return;
        }

        // DispatcherTimer.Tick doesn't wait for an async handler to finish before scheduling the
        // next one — with several rows to poll, a pass can take longer than the tick interval, so
        // without this guard a new pass would start while the previous one's requests are still
        // in flight. RetroArchClient now serializes its own requests regardless, but overlapping
        // passes here would still queue up needless backlog.
        if (_isPolling)
        {
            return;
        }

        _isPolling = true;
        try
        {
            // Snapshot before iterating: this loop awaits network calls per row, yielding control
            // back to the UI thread on every await — if the user adds a row (or anything else
            // mutates Rows) while paused mid-loop, enumerating the live ObservableCollection
            // directly would throw "Collection was modified" and (per the Tick handler above)
            // that used to crash the app outright.
            TrackedValueRowViewModel[] rowsSnapshot = Rows.ToArray();
            foreach (TrackedValueRowViewModel row in rowsSnapshot)
            {
                try
                {
                    if (row.IsFrozen)
                    {
                        // Re-assert the frozen value every tick so the game's own writes don't stick (FR-2.6).
                        await WriteRowAsync(row, row.RawValue).ConfigureAwait(true);
                    }
                    else
                    {
                        uint offset = AddressTranslator.ToCoreOffset(_console, row.TrackedValue.ConsoleAddress);
                        byte[] bytes = await _client.ReadCoreRamAsync(offset, row.DataType.ByteWidth()).ConfigureAwait(true);
                        row.UpdateFromLiveRead(RawValueCodec.FromBytes(bytes, row.TrackedValue.ByteOrder));
                    }
                }
                catch (Exception ex)
                {
                    row.IsLive = false;
                    row.LastError = ex.Message;
                }
            }
        }
        finally
        {
            _isPolling = false;
        }
    }

    private async Task WriteRowAsync(TrackedValueRowViewModel row, uint newRawValue)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Not connected to RetroArch — cannot write live memory. Use the cheat code generator instead (FR-4.4).");
        }

        uint offset = AddressTranslator.ToCoreOffset(_console, row.TrackedValue.ConsoleAddress);
        byte[] bytes = RawValueCodec.ToBytes(newRawValue, row.DataType.ByteWidth(), row.TrackedValue.ByteOrder);
        await _client.WriteCoreRamAsync(offset, bytes).ConfigureAwait(true);
        row.IsLive = true;
        row.LastError = null;

        AuditLog.Insert(0, $"[{DateTime.Now:HH:mm:ss}] Wrote 0x{newRawValue:X} to '{row.Label}' ({row.AddressHex})");
        TrimAuditLog();
    }

    private void TrimAuditLog()
    {
        while (AuditLog.Count > 200)
        {
            AuditLog.RemoveAt(AuditLog.Count - 1);
        }
    }
}
