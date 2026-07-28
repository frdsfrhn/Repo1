using RomMemoryScanner.Core.CheatCodes;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>
/// Which cheat-code family to use for a given console. Only meaningful for SNES/Genesis, which
/// each have two real-world formats: Game Genie (ROM-bus intercept — can't affect RAM) and Pro
/// Action Replay (a direct RAM poke). PS1 only ever had GameShark/PAR, so this choice doesn't
/// apply there. See <see cref="Core.CheatCodes.SnesGameGenieCodec"/> remarks for why the
/// distinction matters for live stat values.
/// </summary>
public enum CheatFormat
{
    GameGenie,
    ProActionReplay,
}

/// <summary>Cheat code generator/decoder (FR-7.1 view 4, FR-5.1-5.3).</summary>
public sealed class CheatCodeViewModel : ViewModelBase
{
    private ConsoleType _console = ConsoleType.Ps1;
    private CheatFormat _format = CheatFormat.ProActionReplay;
    private string _addressHex = "80010000";
    private string _valueHex = "0064";
    private DataType _ps1DataType = DataType.U16;
    private string? _generatedCode;
    private string? _generateError;

    private string _pasteCode = string.Empty;
    private string? _decodedSummary;
    private string? _decodeError;

    public CheatCodeViewModel()
    {
        GenerateCommand = new RelayCommand(_ => Generate());
        DecodeCommand = new RelayCommand(_ => Decode());
    }

    public IEnumerable<ConsoleType> AvailableConsoles => new[] { ConsoleType.Snes, ConsoleType.Genesis, ConsoleType.Ps1 };

    /// <summary>Only relevant when <see cref="Console"/> is SNES or Genesis (PS1 always uses GameShark/PAR).</summary>
    public IEnumerable<CheatFormat> AvailableFormats => new[] { CheatFormat.GameGenie, CheatFormat.ProActionReplay };

    public IEnumerable<DataType> Ps1DataTypes => new[] { DataType.U8, DataType.U16 };

    public ConsoleType Console { get => _console; set => SetField(ref _console, value); }

    public CheatFormat Format { get => _format; set => SetField(ref _format, value); }

    public string AddressHex { get => _addressHex; set => SetField(ref _addressHex, value); }
    public string ValueHex { get => _valueHex; set => SetField(ref _valueHex, value); }
    public DataType Ps1DataType { get => _ps1DataType; set => SetField(ref _ps1DataType, value); }

    public string? GeneratedCode { get => _generatedCode; private set => SetField(ref _generatedCode, value); }
    public string? GenerateError { get => _generateError; private set => SetField(ref _generateError, value); }

    public string PasteCode { get => _pasteCode; set => SetField(ref _pasteCode, value); }
    public string? DecodedSummary { get => _decodedSummary; private set => SetField(ref _decodedSummary, value); }
    public string? DecodeError { get => _decodeError; private set => SetField(ref _decodeError, value); }

    public RelayCommand GenerateCommand { get; }
    public RelayCommand DecodeCommand { get; }

    private void Generate()
    {
        GenerateError = null;
        GeneratedCode = null;
        try
        {
            uint address = Convert.ToUInt32(AddressHex.Trim(), 16);
            uint value = Convert.ToUInt32(ValueHex.Trim(), 16);

            GeneratedCode = (Console, Format) switch
            {
                (ConsoleType.Snes, CheatFormat.GameGenie) => SnesGameGenieCodec.Encode(address, checked((byte)value)),
                (ConsoleType.Snes, CheatFormat.ProActionReplay) => SnesActionReplayCodec.Encode(address, checked((byte)value)),
                (ConsoleType.Genesis, CheatFormat.GameGenie) => GenesisGameGenieCodec.Encode(address, checked((ushort)value)),
                (ConsoleType.Genesis, CheatFormat.ProActionReplay) => GenesisActionReplayCodec.Encode(address, checked((ushort)value)),
                (ConsoleType.Ps1, _) => Ps1GameSharkCodec.Encode(address, value, Ps1DataType),
                _ => throw new InvalidOperationException("Unsupported console/format combination."),
            };
        }
        catch (Exception ex)
        {
            GenerateError = ex.Message;
        }
    }

    private void Decode()
    {
        DecodeError = null;
        DecodedSummary = null;
        try
        {
            CheatDecodeResult result = (Console, Format) switch
            {
                (ConsoleType.Snes, CheatFormat.GameGenie) => SnesGameGenieCodec.Decode(PasteCode),
                (ConsoleType.Snes, CheatFormat.ProActionReplay) => SnesActionReplayCodec.Decode(PasteCode),
                (ConsoleType.Genesis, CheatFormat.GameGenie) => GenesisGameGenieCodec.Decode(PasteCode),
                (ConsoleType.Genesis, CheatFormat.ProActionReplay) => GenesisActionReplayCodec.Decode(PasteCode),
                (ConsoleType.Ps1, _) => Ps1GameSharkCodec.Decode(PasteCode),
                _ => throw new InvalidOperationException("Unsupported console/format combination."),
            };

            DecodedSummary = $"Address 0x{result.ConsoleAddress:X6}, value 0x{result.Value:X} ({result.ValueByteWidth * 8}-bit) — {result.Description}";
        }
        catch (Exception ex)
        {
            DecodeError = ex.Message;
        }
    }
}
