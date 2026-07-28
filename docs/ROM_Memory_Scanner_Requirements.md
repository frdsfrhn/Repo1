# Requirements Document
## Windows Utility: Retro-Console Memory Scanner & Cheat Code Generator

**Document version:** 0.1 (Draft)
**Prepared for:** Farhan
**Status:** For review

---

## 1. Purpose & Background

This document defines requirements for a Windows desktop application that helps users locate, view, and edit in-game character/state values (HP, MP, Gold, Items, etc.) inside ROM-based games running on classic-console emulators (SNES, Sega Genesis/Mega Drive, PlayStation 1, optionally GBA/N64 later).

The app has two operating modes, because the two problems are fundamentally different:

| Mode | What it does | Precedent |
|---|---|---|
| **A — Live Memory Scanner** | Attaches to a *running* emulator process, scans its RAM for candidate addresses matching a known value, narrows down via repeated searches, then lets the user watch/edit the address live | Cheat Engine, RetroArch's built-in Cheat Search |
| **B — Static Cheat Database** | Loads a per-game address map (curated, either bundled or user-supplied) that already tags known addresses as "HP," "Gold," etc., and encodes/decodes them into console-native cheat formats | GameHacking.org / GameShark & Game Genie code lists |

**Design reality check:** There is no way to open an arbitrary ROM file and automatically know that offset `0x1F3A2` is "HP" — that mapping doesn't exist anywhere in the ROM itself in a generic, cross-game-readable form. Any "auto-populate character stats" feature is only possible either (a) through the live-scan workflow where the *user* identifies values by changing them in-game, or (b) by matching the loaded ROM against a pre-built database of known addresses for that specific game/region/version. Both are included below; framing the whole product as "just point it at a ROM and it knows everything" is not achievable and should not be promised in the UI.

---

## 2. Scope

### 2.1 In scope
- Windows 10/11 desktop app (primary platform)
- Read access to running emulator process memory (Mode A)
- Reading `.sav`/save-state files and ROM headers to identify game (title, region, checksum) for Mode B lookups
- A local database/schema for storing known address maps per game
- Hex value viewer/editor with live read/write to emulator memory where technically supported
- Cheat code **generation** in console-native formats (Game Genie for SNES/Genesis, GameShark/Pro Action Replay for PS1) as a fallback when live memory injection isn't supported or the user prefers manual entry
- Support for a defined initial set of popular/well-documented emulators (see §5)

### 2.2 Out of scope (v1)
- ROM downloading, ROM distribution, or any DRM circumvention — the user supplies their own legally-owned ROM dumps
- Online multiplayer/netplay memory manipulation
- Mobile/console builds
- Any online leaderboard/anti-cheat bypass functionality
- Full disassembly/decompilation tooling (this is a memory/cheat tool, not a reverse-engineering IDE — though it may *link out* to tools like Ghidra/IDA for advanced users)

### 2.3 Assumptions & legal notes
- Users are expected to only load ROM files they legally own/have dumped themselves. The app should not host, link to, or facilitate acquisition of ROMs.
- Live memory read/write requires the app to run with sufficient privileges to open a handle to another process (`OpenProcess`/`ReadProcessMemory`/`WriteProcessMemory` on Windows) — this may trigger antivirus/SmartScreen warnings and should be documented for the user.
- Some emulators (notably RetroArch) expose safer, sanctioned interfaces for this (see §5.2) and should be preferred over raw process-memory hacking where possible.

---

## 3. Target Systems (v1)

| Console | Common Windows Emulators (target compatibility) | Native cheat format |
|---|---|---|
| SNES | Snes9x, bsnes/higan, RetroArch (core: snes9x/bsnes) | Game Genie, Pro Action Replay |
| Sega Genesis/Mega Drive | Kega Fusion, Gens/GS, RetroArch (Genesis Plus GX) | Game Genie, Action Replay |
| PlayStation 1 | DuckStation, ePSXe, RetroArch (core: swanstation/duckstation) | GameShark / Pro Action Replay (PAR) |

Stretch goals (post-v1): Game Boy/GBA (VBA-M), N64 (Project64).

---

## 4. Functional Requirements

### 4.1 ROM & Game Identification
- FR-1.1: Detect loaded ROM's console type from file extension/header (`.sfc/.smc`, `.md/.bin/.gen`, `.bin/.cue/.iso`).
- FR-1.2: Compute a checksum/hash (CRC32 and/or MD5) of the ROM to match against the internal game database, so region/version-specific address maps are used correctly.
- FR-1.3: Display identified title, region, and version to the user; allow manual override if auto-ID fails.

### 4.2 Emulator Process Attachment (Mode A)
- FR-2.1: Detect running supported emulator processes on the system.
- FR-2.2: Attach to the selected process and locate its emulated-RAM region in host process memory (see §6.2 for approach per emulator).
- FR-2.3: Provide a live hex/memory viewer of the emulated RAM space (not the whole host process space).
- FR-2.4: Support "value scan" workflow:
  - Initial scan: user provides current known value (e.g., "my HP is 87") and a data type (1/2/4-byte, signed/unsigned, BCD).
  - Next scan: value changed in-game (e.g., took damage), user enters new value or selects "increased/decreased/changed/unchanged," list of candidate addresses narrows.
  - Repeat until a small candidate list (ideally 1) remains.
- FR-2.5: Allow tagging a found address with a friendly label ("Player HP") and data type, saved into a per-game profile for reuse.
- FR-2.6: Allow direct edit of a value at a found/tagged address, written back live to the emulator's memory (freeze/lock option to hold a value constant, e.g. infinite HP).

### 4.3 Static Address Database (Mode B)
- FR-3.1: Bundle a starter database of known address maps for a small number of popular, well-documented titles per console (community cheat-code sites publish these; database should be structured, versioned, and user-extensible).
- FR-3.2: Allow users to import/export address maps (JSON format) to share or back up their own findings.
- FR-3.3: When a ROM is loaded and matched, auto-populate the "known stats" panel (HP/MP/Gold/Items/etc.) from the database, each row showing: label, address (hex), current live value (if attached in Mode A) or last-known/default value, data type.
- FR-3.4: Flag when a database entry's checksum doesn't match the loaded ROM version (different revision/region often shifts addresses) and warn the user rather than silently applying wrong offsets.

### 4.4 Hex Value Editing
- FR-4.1: Display all tracked values in both hex and decimal, editable in either field with automatic conversion.
- FR-4.2: Support common encodings found in retro games: raw binary, BCD (binary-coded decimal — common for HP/Gold displays), and bitflags (for inventory/item-owned flags).
- FR-4.3: Validate edits against the field's byte-width/type before writing, to avoid corrupting adjacent memory.
- FR-4.4: "Apply" writes to live emulator memory (Mode A) if attached; otherwise offers cheat-code generation (below).

### 4.5 Cheat Code Generation (fallback path)
- FR-5.1: Given an address + desired value + console type, generate the equivalent cheat code string in the relevant native format:
  - SNES/Genesis: Game Genie code (console-specific encoding/scrambling algorithm) and/or Pro Action Replay raw address:value format.
  - PS1: GameShark/PAR code (`8-digit address : 4-digit value` format, including RAM offset conventions e.g. `800xxxxx`).
- FR-5.2: Display the generated code in a copy-friendly field so the user can paste it into their emulator's built-in cheat/PAR field.
- FR-5.3: Round-trip: allow pasting an existing Game Genie/GameShark code *in* and decode it back to address+value, for users who found a code online and want to understand/tweak it.

### 4.6 RetroArch Integration (preferred path where available)
- FR-6.1: For RetroArch specifically, use its official Network Command Interface / UDP command port (`READ_CORE_RAM`, `WRITE_CORE_RAM`) instead of raw process-memory hacking — this is safer, sanctioned by the emulator, and less likely to trigger AV false positives.
- FR-6.2: Where a RetroArch core is in use, prefer this integration path automatically and fall back to raw memory access only for standalone emulators that lack such an interface.

### 4.7 UI/UX
- FR-7.1: Main views: (1) Game/Process picker, (2) Live scan workspace, (3) Tagged values dashboard (the "character sheet" style HP/MP/Gold/Items view), (4) Cheat code generator/decoder, (5) Address database browser/editor.
- FR-7.2: Dashboard view should visually resemble a simple stat sheet — labeled rows per tracked value, hex + decimal side by side, edit + freeze toggle per row.
- FR-7.3: Clear status indicator: "Attached to [emulator] — [game title] — [live/static mode]."

---

## 5. Technical Approach by Emulator Type

### 5.1 Standalone emulators (Snes9x, Kega Fusion, ePSXe, DuckStation, etc.)
- Use Windows APIs (`OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`) to access the emulator's process memory.
- Requires locating the emulated console RAM within the host process's virtual address space — typically found via signature/pattern scanning (since heap addresses shift between runs) rather than fixed offsets.
- Each supported emulator version needs its own memory-region-finding logic; this is the most maintenance-heavy part of the project and versions will need periodic updates as emulators update.

### 5.2 RetroArch (preferred where usable)
- RetroArch exposes a UDP-based command interface (commands like `READ_CORE_RAM <addr> <bytes>` / `WRITE_CORE_RAM <addr> <byte> ...`) intended for exactly this kind of external tooling.
- This avoids raw process-memory access entirely and is far more stable across RetroArch updates.
- Recommend this as the primary supported path in v1, with standalone-emulator support as a secondary/expanded milestone.

### 5.3 Console RAM addressing conventions
- SNES: WRAM is typically `0x7E0000`–`0x7FFFFF` in the SNES address space.
- Genesis: 68K work RAM at `0xFF0000`–`0xFFFFFF`.
- PS1: Main RAM at `0x80000000`–`0x801FFFFF` (2MB); GameShark codes conventionally reference this as `800xxxxx`.
- The app's internal address model should store "console-space" addresses (the ones humans/cheat databases use) and translate to "host-process-space" addresses separately, keeping the database format emulator-independent.

---

## 6. Data Model (high level)

```
Game
 ├─ title, console, region, version
 ├─ checksum (CRC32/MD5)
 └─ TrackedValue[]
       ├─ label (e.g. "Player HP")
       ├─ console_address (hex)
       ├─ data_type (u8/u16/u32/BCD/bitflag)
       ├─ byte_order (LE/BE)
       └─ notes
```

- Stored locally as JSON per game, collected into a database folder; user-editable and shareable.
- No cloud sync required for v1; local file import/export only.

---

## 7. Non-Functional Requirements

- **NFR-1 (Safety):** The app must never write outside the identified emulated-RAM boundary; all writes bounds-checked.
- **NFR-2 (Transparency):** Any live memory read/write action should be visible/logged to the user (audit trail of edits made in a session).
- **NFR-3 (Performance):** Live value-scan against several MB of emulated RAM should complete each pass in well under 1 second on typical hardware.
- **NFR-4 (Compatibility):** Must run without requiring the emulator to be modified/patched.
- **NFR-5 (Resilience):** Graceful handling when the emulator process closes/crashes mid-session (no crash, clear "disconnected" state).
- **NFR-6 (Distribution):** Expect AV/SmartScreen friction due to process-memory access; app should be code-signed, and documentation should explain to users why this triggers warnings.

---

## 8. Risks & Open Questions

| Risk | Notes |
|---|---|
| Per-emulator memory-location logic breaks on emulator updates | Needs a maintenance plan / community-sourced pattern updates |
| "Auto-populate stats for any game" is not truly automatic | Must be scoped as "known-game database" + "user-driven live scan," not universal auto-detection — recommend this be made explicit in any marketing/UI copy |
| Antivirus false positives | Memory-editing tools are frequently flagged; needs code-signing and possibly AV vendor allowlisting requests |
| Save corruption from bad writes | Bounds-checking and an "undo last write" / snapshot feature recommended |
| Legal/ToS | Tool itself is legal (no different in kind from Cheat Engine or RetroArch's own cheat search); avoid bundling/linking ROMs |

**Decisions locked for build:**
1. **v1 scope:** RetroArch-only integration first (Phases 1–2 below); standalone-emulator process hacking deferred to Phase 3. Faster to ship, safer, and avoids AV friction out of the gate.
2. **Starter database:** Ship with a small handful (5–10) of well-documented titles per console as proof of concept; the import/export JSON format is designed from day one so community/user contributions are the long-term growth path, not an afterthought.
3. **Tech stack:** **.NET 8, C#, WPF.**
   - WPF over WinForms for cleaner data-bound UI (the tagged-value dashboard needs live-updating rows, which WPF's binding model handles well).
   - Avalonia (cross-platform) considered but not needed — this is Windows-only by requirement.
   - `System.Net.Sockets` (UDP) for the RetroArch command interface — no external dependency needed.
   - P/Invoke (`kernel32.dll`: `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`) reserved for Phase 3 standalone-emulator support.
   - Address database persisted as JSON via `System.Text.Json`.

---

## 8a. Appendix: Cheat Code Encoding Reference

This is the technical basis for FR-5.1–5.3. All formats below are long-established, publicly documented conventions used across the ROM-hacking/emulation community (the same ones RetroArch, Snes9x, and community cheat databases already implement) — nothing proprietary or reverse-engineered here.

### SNES Game Genie
- A code is an 8-character string drawn from the alphabet `DF4709156BC8A23E` (16 symbols → 4 bits each), giving 32 bits total.
- Those 32 bits decode to: a 16-bit **address** (mapped into SNES address space, offset from `0x000000`) and an 8-bit **value** to poke at that address, but the bits are *interleaved/scrambled* in a fixed, well-known bit-permutation pattern (not simple concatenation) — this is what makes raw address:value pairs unreadable as "codes" without running them through the scramble table.
- Implementation approach: hardcode the standard 32-bit permutation table (bit position → bit position mapping), then:
  - **Encode:** address+value → apply permutation → map each 4-bit nibble back to its Game Genie letter.
  - **Decode:** letters → nibbles → reverse permutation → address+value.

### Genesis/Mega Drive Game Genie
- 9-character code, alphabet `ABCDEFGHJKLMNPRSTVWXYZ0123456789` (base-32-ish, excludes ambiguous characters like `O`/`I`/`Q`).
- Encodes a 24-bit address + 16-bit value (larger than SNES since Genesis addressing/values differ), again via a fixed bit-scramble distinct from the SNES one.
- Same encode/decode approach: permutation table lookup, implemented once and unit-tested against known published code/value pairs to confirm correctness before shipping.

### PS1 GameShark / Pro Action Replay (PAR)
- Much simpler — no bit-scrambling. A code line is two 4-byte hex groups: `AAAAAAAA YYYY`
  - `AAAAAAAA` = an 8-hex-digit **code-type + address** field. The leading byte(s) indicate the write type (e.g. `30` = 8-bit write, `80` = 16-bit write) and the rest is the RAM offset, conventionally expressed against PS1 RAM's `0x800xxxxx` base.
  - `YYYY` = the 4-hex-digit value to write (padded appropriately for 8-bit writes).
- This format is a direct address:value pair once the code-type prefix is understood — no permutation step, which makes PS1 the easiest console to support first from an encoding-correctness standpoint. Recommend implementing/validating this one first, then SNES, then Genesis.

### Validation strategy
- For each format, build a small fixture set of known, publicly-documented code ↔ address/value pairs (widely available on community wikis) and unit test encode/decode round-trips against them before trusting the implementation on live data.

---

## 9. Suggested Phased Delivery

1. **Phase 1:** RetroArch integration only, static database + manual tagging, hex dashboard, cheat code generator/decoder for SNES & Genesis Game Genie formats.
2. **Phase 2:** Add PS1 GameShark/PAR format; add live value-scan workflow (Cheat Engine–style) via RetroArch's RAM commands.
3. **Phase 3:** Add standalone emulator support (Snes9x, DuckStation, etc.) via process-memory access.
4. **Phase 4:** Community database import/export, sharing format, expand console list (GBA/N64).

---

*End of draft. Tech stack and phasing are locked (§8); encoding reference is in §8a. Ready to hand off to Claude Code for scaffolding — suggest starting with Phase 1 (RetroArch + PS1 GameShark decode/encode, since it's the simplest format to validate first).*
