using System.Text.Json.Serialization;

namespace RomMemoryScanner.Core.Models;

/// <summary>
/// A per-game address map, per §6. Stored as one JSON file per game under the database folder
/// (FR-3.1); user-editable and shareable via import/export (FR-3.2).
/// </summary>
public sealed class Game
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("console")]
    public ConsoleType Console { get; set; } = ConsoleType.Unknown;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("checksum")]
    public RomChecksum Checksum { get; set; } = new();

    [JsonPropertyName("tracked_values")]
    public List<TrackedValue> TrackedValues { get; set; } = new();

    /// <summary>
    /// Schema/content version of this database entry, bumped by contributors (FR-3.1: "structured,
    /// versioned, user-extensible"). Not the game's own release version — see <see cref="Version"/>.
    /// </summary>
    [JsonPropertyName("entry_schema_version")]
    public int EntrySchemaVersion { get; set; } = 1;
}
