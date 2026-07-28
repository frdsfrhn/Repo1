namespace RomMemoryScanner.Core.Scanning;

/// <summary>
/// Narrowing operators for the next-scan step of the value-scan workflow (FR-2.4): after the
/// initial known-value scan, the user either enters a new known value (<see cref="EqualTo"/>) or
/// tells the app how the value changed relative to the previous pass.
/// </summary>
public enum ScanComparison
{
    EqualTo,
    Increased,
    Decreased,
    Changed,
    Unchanged,
}
