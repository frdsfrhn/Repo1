# Address database

This folder is the local, file-based address database described in requirements §3.4 / FR-3.1.
`RomMemoryScanner.Core.Database.GameDatabaseService` loads every `*.json` file found anywhere
under here (recursively), matches loaded ROMs against entries by CRC32/MD5 (FR-1.2, FR-3.3), and
flags entries whose checksum doesn't match the loaded ROM instead of silently applying possibly-
wrong offsets (FR-3.4).

## About `starter/`

The requirements doc (§8, "Decisions locked for build" #2) calls for shipping "a small handful
(5-10) of well-documented titles per console as proof of concept." The entries under `starter/`
are intentionally **schema templates with placeholder addresses**, not verified real-game data —
producing a database entry that *looks* authoritative but has an unverified or wrong address is
worse than shipping none, since a user could freeze/overwrite the wrong memory location believing
it's "Player HP." (This is exactly the failure mode FR-3.4's checksum-mismatch warning exists to
catch for cross-revision drift, but it can't catch an address that was simply wrong to begin with.)

Each template documents where a contributor should get verified values for a real title:
community sites such as GameHacking.org's code/address listings, or values the user finds
themselves via the Mode A live-scan workflow (§4.2) and tags for reuse (FR-2.5) — which is the
one workflow in this app that produces addresses with zero risk of database error, since the user
confirmed them against their own running game.

## Schema

```jsonc
{
  "title": "Game Title",
  "console": "Snes" | "Genesis" | "Ps1",
  "region": "USA" | "Europe" | "Japan" | ...,
  "version": "1.0",
  "checksum": { "crc32": "hex string", "md5": "hex string" },
  "entry_schema_version": 1,
  "tracked_values": [
    {
      "label": "Player HP",
      "console_address": 8257456,   // uint, console-space address (see §5.3) — hex literals
                                     // aren't valid JSON, so store as a plain integer; the app's
                                     // UI displays/accepts it in hex.
      "data_type": "U16" | "U8" | "U32" | "S8" | "S16" | "S32"
                  | "Bcd8" | "Bcd16" | "Bcd32" | "Bitflag8" | "Bitflag16" | "Bitflag32",
      "byte_order": "LittleEndian" | "BigEndian",
      "notes": "optional free text"
    }
  ]
}
```

## Contributing verified entries

Use FR-3.2's import/export: build/edit a `Game` JSON file (by hand or via the app's database
browser, once built), verify addresses against your own live-scan findings or a community
source you trust, and drop the file under the appropriate `starter/<console>/` folder (or your
own local folder — the loader scans everywhere under `database/`).
