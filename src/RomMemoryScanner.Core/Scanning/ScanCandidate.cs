namespace RomMemoryScanner.Core.Scanning;

/// <summary>One surviving candidate address from a value-scan pass, with its current raw value.</summary>
public sealed record ScanCandidate(uint ConsoleAddress, uint Value);
