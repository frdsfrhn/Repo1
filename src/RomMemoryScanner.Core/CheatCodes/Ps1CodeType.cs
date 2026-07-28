namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>
/// PS1 GameShark/Pro Action Replay code-type prefixes (the leading byte of the 8-digit address
/// field), per §8a. This is a long-established, publicly-documented convention with no
/// scrambling — the address+value pair is direct once the prefix is understood.
/// </summary>
public enum Ps1CodeType : byte
{
    ConstantWrite16 = 0x80,
    ConstantWrite8 = 0x30,
    Increment16Once = 0x10,
    Decrement16Once = 0x11,
    Increment8Once = 0x20,
    Decrement8Once = 0x21,
    IfEqual16 = 0xD0,
    IfNotEqual16 = 0xD1,
    IfLess16 = 0xD2,
    IfGreater16 = 0xD3,
    IfEqual8 = 0xE0,
    IfNotEqual8 = 0xE1,
    IfLess8 = 0xE2,
    IfGreater8 = 0xE3,
}

public static class Ps1CodeTypeExtensions
{
    public static int ValueByteWidth(this Ps1CodeType type) => type switch
    {
        Ps1CodeType.ConstantWrite16 or Ps1CodeType.Increment16Once or Ps1CodeType.Decrement16Once
            or Ps1CodeType.IfEqual16 or Ps1CodeType.IfNotEqual16 or Ps1CodeType.IfLess16 or Ps1CodeType.IfGreater16 => 2,
        _ => 1,
    };

    public static string Describe(this Ps1CodeType type) => type switch
    {
        Ps1CodeType.ConstantWrite16 => "16-bit constant write",
        Ps1CodeType.ConstantWrite8 => "8-bit constant write",
        Ps1CodeType.Increment16Once => "16-bit increment once",
        Ps1CodeType.Decrement16Once => "16-bit decrement once",
        Ps1CodeType.Increment8Once => "8-bit increment once",
        Ps1CodeType.Decrement8Once => "8-bit decrement once",
        Ps1CodeType.IfEqual16 => "16-bit conditional: equal",
        Ps1CodeType.IfNotEqual16 => "16-bit conditional: not equal",
        Ps1CodeType.IfLess16 => "16-bit conditional: less than",
        Ps1CodeType.IfGreater16 => "16-bit conditional: greater than",
        Ps1CodeType.IfEqual8 => "8-bit conditional: equal",
        Ps1CodeType.IfNotEqual8 => "8-bit conditional: not equal",
        Ps1CodeType.IfLess8 => "8-bit conditional: less than",
        Ps1CodeType.IfGreater8 => "8-bit conditional: greater than",
        _ => "Unknown code type",
    };
}
