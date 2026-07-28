using RomMemoryScanner.Core.CheatCodes;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>Cheat code generator/decoder (FR-7.1 view 4, FR-5.1-5.3).</summary>
public sealed class CheatCodeViewModel : ViewModelBase
{
    private ConsoleType _console = ConsoleType.Ps1;
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

    public IEnumerable<DataType> Ps1DataTypes => new[] { DataType.U8, DataType.U16 };

    public ConsoleType Console { get => _console; set => SetField(ref _console, value); }
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

            GeneratedCode = Console switch
            {
                ConsoleType.Snes => SnesGameGenieCodec.Encode(address, checked((byte)value)),
                ConsoleType.Genesis => GenesisGameGenieCodec.Encode(address, checked((ushort)value)),
                ConsoleType.Ps1 => Ps1GameSharkCodec.Encode(address, value, Ps1DataType),
                _ => throw new InvalidOperationException("Unsupported console."),
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
            CheatDecodeResult result = Console switch
            {
                ConsoleType.Snes => SnesGameGenieCodec.Decode(PasteCode),
                ConsoleType.Genesis => GenesisGameGenieCodec.Decode(PasteCode),
                ConsoleType.Ps1 => Ps1GameSharkCodec.Decode(PasteCode),
                _ => throw new InvalidOperationException("Unsupported console."),
            };

            DecodedSummary = $"Address 0x{result.ConsoleAddress:X6}, value 0x{result.Value:X} ({result.ValueByteWidth * 8}-bit) — {result.Description}";
        }
        catch (Exception ex)
        {
            DecodeError = ex.Message;
        }
    }
}
