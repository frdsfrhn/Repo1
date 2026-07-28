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

    public DashboardViewModel()
    {
        Rows = new ObservableCollection<TrackedValueRowViewModel>();
        AuditLog = new ObservableCollection<string>();
        _pollTimer = new DispatcherTimer { Interval = PollInterval };
        _pollTimer.Tick += async (_, _) => await PollAsync().ConfigureAwait(true);
    }

    public ObservableCollection<TrackedValueRowViewModel> Rows { get; }

    /// <summary>NFR-2: any live memory read/write action is logged, visible to the user as an audit trail.</summary>
    public ObservableCollection<string> AuditLog { get; }

    public string? GameTitle
    {
        get => _gameTitle;
        private set => SetField(ref _gameTitle, value);
    }

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

    /// <summary>FR-2.5: lets a value found via live-scan (Phase 2) be tagged and added here; also usable for manual tagging today.</summary>
    public void AddTrackedValue(TrackedValue trackedValue)
    {
        Rows.Add(new TrackedValueRowViewModel(trackedValue, WriteRowAsync));
    }

    private async Task PollAsync()
    {
        if (_client is null)
        {
            return;
        }

        foreach (TrackedValueRowViewModel row in Rows)
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
