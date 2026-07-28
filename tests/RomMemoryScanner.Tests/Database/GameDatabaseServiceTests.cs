using RomMemoryScanner.Core.Database;
using RomMemoryScanner.Core.Models;
using Xunit;

namespace RomMemoryScanner.Tests.Database;

public class GameDatabaseServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "rms-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static Game SampleGame() => new()
    {
        Title = "Example Quest",
        Console = ConsoleType.Snes,
        Region = "USA",
        Version = "1.0",
        Checksum = new RomChecksum { Crc32 = "deadbeef", Md5 = "0123456789abcdef0123456789abcdef" },
        TrackedValues =
        {
            new TrackedValue { Label = "Player HP", ConsoleAddress = 0x7E0DB0, DataType = DataType.U16, ByteOrder = ByteOrder.LittleEndian },
        },
    };

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        Game original = SampleGame();
        string path = Path.Combine(_tempRoot, "snes", "example-quest.json");

        GameDatabaseService.SaveToFile(original, path);
        Game loaded = GameDatabaseService.LoadFromFile(path);

        Assert.Equal(original.Title, loaded.Title);
        Assert.Equal(original.Console, loaded.Console);
        Assert.Equal(original.Checksum.Crc32, loaded.Checksum.Crc32);
        Assert.Single(loaded.TrackedValues);
        Assert.Equal(original.TrackedValues[0].Label, loaded.TrackedValues[0].Label);
        Assert.Equal(original.TrackedValues[0].ConsoleAddress, loaded.TrackedValues[0].ConsoleAddress);
        Assert.Equal(original.TrackedValues[0].DataType, loaded.TrackedValues[0].DataType);
    }

    [Fact]
    public void LoadAll_FindsGamesAcrossSubdirectories()
    {
        GameDatabaseService.SaveToFile(SampleGame(), Path.Combine(_tempRoot, "snes", "a.json"));
        Game other = SampleGame();
        other.Title = "Second Game";
        other.Checksum = new RomChecksum { Crc32 = "cafef00d" };
        GameDatabaseService.SaveToFile(other, Path.Combine(_tempRoot, "genesis", "b.json"));

        var service = new GameDatabaseService(_tempRoot);
        IReadOnlyList<Game> games = service.LoadAll();

        Assert.Equal(2, games.Count);
    }

    [Fact]
    public void LoadAll_ReturnsEmpty_WhenDirectoryDoesNotExist()
    {
        var service = new GameDatabaseService(Path.Combine(_tempRoot, "does-not-exist"));
        Assert.Empty(service.LoadAll());
    }

    [Fact]
    public void FindByChecksum_MatchesOnCrc32()
    {
        Game game = SampleGame();
        var romChecksum = new RomChecksum { Crc32 = "DEADBEEF" }; // different case, should still match

        Game? found = GameDatabaseService.FindByChecksum(new[] { game }, ConsoleType.Snes, romChecksum);

        Assert.NotNull(found);
        Assert.Equal(game.Title, found!.Title);
    }

    [Fact]
    public void FindByChecksum_ReturnsNull_WhenNoEntryMatches()
    {
        Game game = SampleGame();
        var romChecksum = new RomChecksum { Crc32 = "00000000" };

        Game? found = GameDatabaseService.FindByChecksum(new[] { game }, ConsoleType.Snes, romChecksum);

        Assert.Null(found);
    }

    [Fact]
    public void FindChecksumMismatches_FlagsSameTitleDifferentChecksum()
    {
        Game game = SampleGame();
        var loadedRomChecksum = new RomChecksum { Crc32 = "11111111" }; // different revision

        IReadOnlyList<Game> mismatches = GameDatabaseService.FindChecksumMismatches(
            new[] { game }, ConsoleType.Snes, game.Title, loadedRomChecksum);

        Assert.Single(mismatches);
    }

    [Fact]
    public void Import_ThenExport_ProducesReloadableFile()
    {
        Game original = SampleGame();
        string exportPath = Path.Combine(_tempRoot, "exported.json");
        GameDatabaseService.Export(original, exportPath);

        Game imported = GameDatabaseService.Import(exportPath);

        Assert.Equal(original.Title, imported.Title);
    }
}
