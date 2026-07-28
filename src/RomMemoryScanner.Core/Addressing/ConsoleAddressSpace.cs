using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.Addressing;

/// <summary>
/// Console-space RAM conventions per §5.3. These are the addresses humans and cheat databases
/// use (e.g. SNES WRAM starting at 0x7E0000) — distinct from the core-relative offsets that
/// RetroArch's READ_CORE_RAM/WRITE_CORE_RAM commands expect (see <see cref="AddressTranslator"/>).
/// </summary>
public sealed record ConsoleAddressSpace(ConsoleType Console, uint WramBase, uint WramSize)
{
    public uint WramEndInclusive => WramBase + WramSize - 1;

    public bool ContainsConsoleAddress(uint consoleAddress) =>
        consoleAddress >= WramBase && consoleAddress <= WramEndInclusive;

    public static readonly ConsoleAddressSpace Snes = new(ConsoleType.Snes, WramBase: 0x7E0000, WramSize: 0x020000);

    public static readonly ConsoleAddressSpace Genesis = new(ConsoleType.Genesis, WramBase: 0xFF0000, WramSize: 0x010000);

    /// <summary>
    /// PS1 main RAM as conventionally addressed by GameShark/PAR codes: the cached KSEG0 mirror
    /// at 0x80000000-0x801FFFFF (2MB), per §5.3. The underlying physical RAM (KUSEG,
    /// 0x00000000-0x001FFFFF) is the same bytes; only the top address bits differ.
    /// </summary>
    public static readonly ConsoleAddressSpace Ps1 = new(ConsoleType.Ps1, WramBase: 0x80000000, WramSize: 0x00200000);

    public static ConsoleAddressSpace For(ConsoleType console) => console switch
    {
        ConsoleType.Snes => Snes,
        ConsoleType.Genesis => Genesis,
        ConsoleType.Ps1 => Ps1,
        _ => throw new ArgumentOutOfRangeException(nameof(console), console, "No known WRAM convention for this console."),
    };
}
