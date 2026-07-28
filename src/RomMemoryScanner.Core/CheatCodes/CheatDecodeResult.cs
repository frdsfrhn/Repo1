namespace RomMemoryScanner.Core.CheatCodes;

/// <summary>Result of decoding a cheat code string back into its console-space address and value (FR-5.3).</summary>
public sealed record CheatDecodeResult(uint ConsoleAddress, uint Value, int ValueByteWidth, string Description);
