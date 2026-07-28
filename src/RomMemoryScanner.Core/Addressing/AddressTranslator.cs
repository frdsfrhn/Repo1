using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.Addressing;

/// <summary>
/// Translates between console-space addresses (what the database and cheat codes use) and the
/// core-relative RAM offsets RetroArch's READ_CORE_RAM/WRITE_CORE_RAM commands expect, which are
/// zero-based from the start of the core's exposed work-RAM block (§5.2, §5.3). All writes are
/// bounds-checked against the console's known WRAM window (NFR-1) — this module is the single
/// place that boundary is enforced, so both Mode A live access and cheat-code generation share it.
/// </summary>
public static class AddressTranslator
{
    public static uint ToCoreOffset(ConsoleType console, uint consoleAddress)
    {
        ConsoleAddressSpace space = ConsoleAddressSpace.For(console);
        if (!space.ContainsConsoleAddress(consoleAddress))
        {
            throw new AddressOutOfRangeException(console, consoleAddress, space);
        }

        return consoleAddress - space.WramBase;
    }

    public static uint ToConsoleAddress(ConsoleType console, uint coreOffset)
    {
        ConsoleAddressSpace space = ConsoleAddressSpace.For(console);
        if (coreOffset >= space.WramSize)
        {
            throw new AddressOutOfRangeException(console, space.WramBase + coreOffset, space);
        }

        return space.WramBase + coreOffset;
    }

    public static bool TryToCoreOffset(ConsoleType console, uint consoleAddress, out uint coreOffset)
    {
        ConsoleAddressSpace space = ConsoleAddressSpace.For(console);
        if (!space.ContainsConsoleAddress(consoleAddress))
        {
            coreOffset = 0;
            return false;
        }

        coreOffset = consoleAddress - space.WramBase;
        return true;
    }
}

/// <summary>
/// Thrown when a console-space address falls outside the console's known WRAM window (NFR-1: the
/// app must never write outside the identified emulated-RAM boundary).
/// </summary>
public sealed class AddressOutOfRangeException : Exception
{
    public ConsoleType Console { get; }
    public uint ConsoleAddress { get; }

    public AddressOutOfRangeException(ConsoleType console, uint consoleAddress, ConsoleAddressSpace space)
        : base($"Address 0x{consoleAddress:X6} is outside {console} WRAM (0x{space.WramBase:X6}-0x{space.WramEndInclusive:X6}).")
    {
        Console = console;
        ConsoleAddress = consoleAddress;
    }
}
