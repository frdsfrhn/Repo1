using RomMemoryScanner.Core.Models;
using RomMemoryScanner.Core.Numeric;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>
/// One row of the tagged-values dashboard (FR-7.2): a <see cref="TrackedValue"/> plus its live
/// state (current value, freeze toggle) and hex/decimal editing (FR-4.1). Read/write to the
/// emulator is delegated to callbacks supplied by <c>DashboardViewModel</c>, which owns the
/// RetroArch connection — this class only knows about a single console-space address and a value.
/// </summary>
public sealed class TrackedValueRowViewModel : ViewModelBase
{
    private readonly Func<TrackedValueRowViewModel, uint, Task> _writeAsync;

    private uint _rawValue;
    private bool _isFrozen;
    private bool _isLive;
    private bool _isEnabled = true;
    private string? _lastError;

    public TrackedValueRowViewModel(TrackedValue trackedValue, Func<TrackedValueRowViewModel, uint, Task> writeAsync)
    {
        TrackedValue = trackedValue;
        _writeAsync = writeAsync;
    }

    public TrackedValue TrackedValue { get; }

    public string Label => TrackedValue.Label;

    public string AddressHex => $"0x{TrackedValue.ConsoleAddress:X6}";

    public DataType DataType => TrackedValue.DataType;

    /// <summary>The raw byte-width value as read from memory (pre BCD-decode).</summary>
    public uint RawValue
    {
        get => _rawValue;
        private set
        {
            if (SetField(ref _rawValue, value))
            {
                OnPropertyChanged(nameof(HexText));
                OnPropertyChanged(nameof(DecimalText));
            }
        }
    }

    public bool IsLive
    {
        get => _isLive;
        set => SetField(ref _isLive, value);
    }

    public string? LastError
    {
        get => _lastError;
        set => SetField(ref _lastError, value);
    }

    public bool IsFrozen
    {
        get => _isFrozen;
        set => SetField(ref _isFrozen, value);
    }

    /// <summary>When off, this row is skipped by the poll loop entirely (no read or write) — pauses tracking without removing the row.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetField(ref _isEnabled, value);
    }

    /// <summary>Called by DashboardViewModel after a successful live read; does not trigger a write-back.</summary>
    public void UpdateFromLiveRead(uint rawValue)
    {
        RawValue = rawValue;
        IsLive = true;
        LastError = null;
    }

    public string HexText
    {
        get => RawValue.ToString("X" + DataType.ByteWidth() * 2);
        set
        {
            if (!uint.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out uint parsed))
            {
                LastError = $"'{value}' is not valid hex.";
                return;
            }

            _ = ApplyAsync(parsed);
        }
    }

    public string DecimalText
    {
        get
        {
            if (IsBcd)
            {
                return BcdConverter.BcdToDecimal(RawValue, DataType.ByteWidth()).ToString();
            }

            if (IsSigned)
            {
                return ToSigned(RawValue, DataType.ByteWidth()).ToString();
            }

            return RawValue.ToString();
        }
        set
        {
            uint raw;
            try
            {
                if (IsBcd)
                {
                    if (!uint.TryParse(value, out uint parsedUnsigned))
                    {
                        LastError = $"'{value}' is not a valid decimal number.";
                        return;
                    }

                    raw = BcdConverter.DecimalToBcd(parsedUnsigned, DataType.ByteWidth());
                }
                else if (IsSigned)
                {
                    if (!int.TryParse(value, out int parsedSigned))
                    {
                        LastError = $"'{value}' is not a valid decimal number.";
                        return;
                    }

                    raw = FromSigned(parsedSigned, DataType.ByteWidth());
                }
                else
                {
                    if (!uint.TryParse(value, out uint parsedUnsigned))
                    {
                        LastError = $"'{value}' is not a valid decimal number.";
                        return;
                    }

                    raw = parsedUnsigned;
                }
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or ArgumentException)
            {
                LastError = ex.Message;
                return;
            }

            _ = ApplyAsync(raw);
        }
    }

    private bool IsBcd => DataType is DataType.Bcd8 or DataType.Bcd16 or DataType.Bcd32;
    private bool IsSigned => DataType is DataType.S8 or DataType.S16 or DataType.S32;

    private static int ToSigned(uint raw, int byteWidth) => byteWidth switch
    {
        1 => (sbyte)raw,
        2 => (short)raw,
        _ => (int)raw,
    };

    private static uint FromSigned(int value, int byteWidth)
    {
        long min = byteWidth switch { 1 => sbyte.MinValue, 2 => short.MinValue, _ => int.MinValue };
        long max = byteWidth switch { 1 => sbyte.MaxValue, 2 => short.MaxValue, _ => int.MaxValue };
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Value does not fit in a signed {byteWidth * 8}-bit field ({min}..{max}).");
        }

        return byteWidth switch
        {
            1 => (byte)(sbyte)value,
            2 => unchecked((ushort)(short)value),
            _ => unchecked((uint)value),
        };
    }

    private async Task ApplyAsync(uint newRawValue)
    {
        // FR-4.3: validate against the field's byte-width before writing, so a bad edit can't
        // corrupt adjacent memory.
        uint maxValue = DataType.ByteWidth() switch
        {
            1 => 0xFFu,
            2 => 0xFFFFu,
            _ => 0xFFFFFFFFu,
        };

        if (newRawValue > maxValue)
        {
            LastError = $"Value 0x{newRawValue:X} does not fit in {DataType.ByteWidth()} byte(s).";
            return;
        }

        try
        {
            await _writeAsync(this, newRawValue).ConfigureAwait(true);
            RawValue = newRawValue;
            LastError = null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }
    }
}
