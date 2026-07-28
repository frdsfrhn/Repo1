namespace RomMemoryScanner.App.ViewModels;

/// <summary>
/// Placeholder for the Mode A value-scan workspace (FR-2.4): initial scan against a known value,
/// then repeated "increased/decreased/changed/unchanged" narrowing passes until a small candidate
/// list remains. Deliberately not implemented yet — per the requirements doc's locked phasing
/// (§9), Phase 1 ships RetroArch integration + static database + dashboard + SNES/Genesis Game
/// Genie codec only; live value-scan lands in Phase 2 alongside the PS1 GameShark format.
/// </summary>
public sealed class LiveScanViewModel : ViewModelBase
{
    public string PhaseNotice =>
        "Live value-scan (Cheat Engine-style narrowing) ships in Phase 2, per the locked delivery plan. " +
        "For now, find addresses by watching community databases (Address Database tab) or by tagging " +
        "known addresses manually once you've found them another way.";
}
