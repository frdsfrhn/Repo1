using System.Text.Json;
using System.Text.Json.Serialization;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.Core.Database;

/// <summary>
/// Loads/saves the local, per-game JSON address database (FR-3.1) and supports user import/export
/// (FR-3.2) so community contributions are a first-class workflow rather than an afterthought.
/// </summary>
public sealed class GameDatabaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _databaseRoot;

    public GameDatabaseService(string databaseRoot)
    {
        _databaseRoot = databaseRoot;
    }

    /// <summary>Loads every *.json game entry found anywhere under the database root, recursively.</summary>
    public IReadOnlyList<Game> LoadAll()
    {
        if (!Directory.Exists(_databaseRoot))
        {
            return Array.Empty<Game>();
        }

        var games = new List<Game>();
        foreach (string file in Directory.EnumerateFiles(_databaseRoot, "*.json", SearchOption.AllDirectories))
        {
            games.Add(LoadFromFile(file));
        }

        return games;
    }

    public static Game LoadFromFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Game>(json, JsonOptions)
            ?? throw new InvalidDataException($"'{filePath}' did not deserialize to a valid game entry.");
    }

    public static void SaveToFile(Game game, string filePath)
    {
        string? directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(game, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    /// <summary>FR-3.2: import a user-supplied or shared address map.</summary>
    public static Game Import(string filePath) => LoadFromFile(filePath);

    /// <summary>FR-3.2: export a game's address map for sharing/backup.</summary>
    public static void Export(Game game, string filePath) => SaveToFile(game, filePath);

    /// <summary>
    /// FR-3.3: finds the database entry matching a loaded ROM's checksum, for the given console.
    /// Returns null if no entry matches — callers should not assume a game is always found (§1
    /// design reality check: there is no universal auto-detection).
    /// </summary>
    public static Game? FindByChecksum(IEnumerable<Game> games, ConsoleType console, RomChecksum romChecksum) =>
        games.FirstOrDefault(g => g.Console == console && g.Checksum.Matches(romChecksum));

    /// <summary>
    /// FR-3.4: finds entries for the same title/console whose stored checksum does NOT match the
    /// loaded ROM — typically a different revision/region where offsets often shift. Callers
    /// should warn the user rather than silently applying these entries' addresses.
    /// </summary>
    public static IReadOnlyList<Game> FindChecksumMismatches(IEnumerable<Game> games, ConsoleType console, string title, RomChecksum romChecksum) =>
        games.Where(g => g.Console == console
                       && string.Equals(g.Title, title, StringComparison.OrdinalIgnoreCase)
                       && !g.Checksum.Matches(romChecksum))
             .ToList();
}
