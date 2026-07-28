using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.Identification;

/// <summary>Result of identifying a loaded ROM/disc image before database lookup (FR-1.1-1.3).</summary>
public sealed record IdentifiedRom(ConsoleType Console, RomChecksum Checksum, string SourcePath)
{
    /// <summary>True when the extension didn't map to any known console and the user must pick one manually.</summary>
    public bool RequiresManualConsoleSelection => Console == ConsoleType.Unknown;
}

/// <summary>Detects a ROM/disc image's console type from its file extension (FR-1.1) and computes its checksum.</summary>
public static class RomIdentifier
{
    private static readonly Dictionary<string, ConsoleType> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // SNES
        [".sfc"] = ConsoleType.Snes,
        [".smc"] = ConsoleType.Snes,
        // Genesis / Mega Drive
        [".md"] = ConsoleType.Genesis,
        [".gen"] = ConsoleType.Genesis,
        // ".bin" is ambiguous (used by both Genesis ROM dumps and PS1 disc images) and is
        // deliberately excluded here; callers should fall back to manual selection for it.
        // PS1 disc images
        [".cue"] = ConsoleType.Ps1,
        [".iso"] = ConsoleType.Ps1,
    };

    public static ConsoleType DetectConsole(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        return ExtensionMap.TryGetValue(extension, out ConsoleType console) ? console : ConsoleType.Unknown;
    }

    public static IdentifiedRom Identify(string filePath)
    {
        ConsoleType console = DetectConsole(filePath);
        using FileStream stream = File.OpenRead(filePath);
        RomChecksum checksum = ChecksumCalculator.Compute(stream);
        return new IdentifiedRom(console, checksum, filePath);
    }
}
