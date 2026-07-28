# ROM Memory Scanner & Cheat Code Generator

A Windows desktop utility for locating, viewing, and editing in-game character/state values (HP,
MP, Gold, Items, etc.) inside ROM-based games running on classic-console emulators, and for
generating/decoding console-native cheat codes. Full requirements: [`docs/ROM_Memory_Scanner_Requirements.md`](docs/ROM_Memory_Scanner_Requirements.md).

**Legal/scope note:** this tool does not host, distribute, or help acquire ROMs. It operates on
ROM/disc images the user already has. Live memory access only talks to RetroArch's own sanctioned
network command interface — no process injection or DRM circumvention involved.

## Status: Phase 1 (of 4), plus Phase 2's live scan pulled forward

Per the requirements doc's locked delivery plan (§9):

- ✅ **Phase 1**: RetroArch integration, static address database with import/export,
  tagged-values dashboard, SNES + Genesis Game Genie codec.
- ✅ *(pulled forward, since it's the simplest format to validate first — see §8a)*:
  PS1 GameShark/Pro Action Replay codec.
- ✅ *(pulled forward from Phase 2)*: SNES/Genesis Pro Action Replay codecs — added after
  real-world testing showed Game Genie can't affect RAM-resident values (see commit history);
  PAR is the format that actually works for live stats like HP/inventory.
- ✅ *(pulled forward from Phase 2)*: **Live value-scan workflow** (FR-2.4) — the Live Scan tab.
  Give it a known value (e.g. "my HP is 87"), it scans all of WRAM for matches, then narrows
  across passes as you tell it the value increased/decreased/changed/stayed the same (or give a
  new known value directly), until one address remains. Tag it and it appears live on the
  Dashboard (FR-2.5). This is how you find HP/MP/Gold addresses for a game with no database entry.
- ⬜ **Phase 3**: Standalone-emulator support (Snes9x, DuckStation, etc.) via raw process memory.
- ⬜ **Phase 4**: Community database sharing conventions, GBA/N64.

## Solution layout

```
RomMemoryScanner.sln
src/
  RomMemoryScanner.Core/    Platform-independent logic: models, checksum/ROM ID, address
                            translation, cheat-code codecs, RetroArch UDP client, JSON database.
  RomMemoryScanner.App/     WPF UI (net8.0-windows). Five views per FR-7.1: game/process picker,
                            live value-scan, tagged-values dashboard, cheat code
                            generator/decoder, address database browser.
tests/
  RomMemoryScanner.Tests/   xunit tests for everything in Core, including the SNES/Genesis Game
                            Genie bit-permutation codecs and the PS1 GameShark/PAR codec.
database/
  starter/                 Schema template entries per console (see database/README.md — these
                            are placeholders to demonstrate the format, not verified real-game data).
```

## Building

**Core + Tests are cross-platform** (`net8.0`, no Windows dependency):

```
dotnet test tests/RomMemoryScanner.Tests/RomMemoryScanner.Tests.csproj
```

**The App project requires Windows.** WPF's SDK targets (`Microsoft.NET.Sdk.WindowsDesktop`) are
only distributed through the `dotnet workload install wpf` mechanism, which Microsoft gates to
Windows hosts — there is no NuGet-only way to build or run it on Linux/macOS. This repo was
developed in a Linux sandbox, so **the App project's XAML/C# has not been compiler-verified** —
only hand-reviewed. To build and run it:

```
dotnet workload install wpf     # Windows only
dotnet build src/RomMemoryScanner.App/RomMemoryScanner.App.csproj
dotnet run --project src/RomMemoryScanner.App/RomMemoryScanner.App.csproj
```

The `database/` folder is copied next to the built exe automatically (see the `Content` item in
`RomMemoryScanner.App.csproj`), so `GameDatabaseService` finds it at `AppContext.BaseDirectory\database`.

## Using it

1. In RetroArch: **Settings > Network > Network Commands** → on. Load a core/game as usual.
2. In the app's **Game/Process** tab: Connect (defaults to `127.0.0.1:55355`), then browse to your
   ROM/disc image to identify it and match it against the address database.
3. No database entry for your game? Use the **Live Scan** tab: enter a value you can see on
   screen (e.g. current HP), Start scan, then play a bit so it changes, tell the app how
   (increased/decreased/changed/unchanged, or a new known value), Next scan — repeat until the
   candidate list narrows to the address you want, then tag it (FR-2.4/FR-2.5).
4. **Dashboard** tab shows any matched or tagged tracked values live, with hex/decimal editing and
   a per-row freeze toggle (FR-2.6).
5. **Cheat Codes** tab generates/decodes SNES/Genesis Game Genie *and* Pro Action Replay, plus PS1
   GameShark/PAR, independent of any live connection (FR-5.1-5.3). Game Genie only affects
   ROM/cartridge reads, not RAM — use Pro Action Replay for live stats like HP/inventory.
6. **Address Database** tab browses/imports/exports the local JSON database (FR-3.1-3.2). Once
   you've tagged values via live-scan, export them here to save/share a real database entry.
