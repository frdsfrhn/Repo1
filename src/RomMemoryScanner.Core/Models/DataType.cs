namespace RomMemoryScanner.Core.Models;

/// <summary>Value encodings found in retro games, per FR-4.2.</summary>
public enum DataType
{
    U8,
    U16,
    U32,
    S8,
    S16,
    S32,
    /// <summary>Binary-coded decimal, one decimal digit per nibble. Common for on-screen HP/Gold counters.</summary>
    Bcd8,
    Bcd16,
    Bcd32,
    /// <summary>Each bit represents an owned/enabled flag (e.g. inventory items).</summary>
    Bitflag8,
    Bitflag16,
    Bitflag32,
}

public static class DataTypeExtensions
{
    public static int ByteWidth(this DataType type) => type switch
    {
        DataType.U8 or DataType.S8 or DataType.Bcd8 or DataType.Bitflag8 => 1,
        DataType.U16 or DataType.S16 or DataType.Bcd16 or DataType.Bitflag16 => 2,
        DataType.U32 or DataType.S32 or DataType.Bcd32 or DataType.Bitflag32 => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
