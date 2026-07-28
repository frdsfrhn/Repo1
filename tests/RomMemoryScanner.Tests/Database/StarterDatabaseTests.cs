using RomMemoryScanner.Core.Database;
using Xunit;

namespace RomMemoryScanner.Tests.Database;

/// <summary>Guards against the shipped starter database templates silently rotting / becoming invalid JSON.</summary>
public class StarterDatabaseTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "RomMemoryScanner.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Could not locate repo root (RomMemoryScanner.sln) from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void AllStarterTemplates_LoadWithoutError_AndHaveAtLeastOneTrackedValue()
    {
        string starterRoot = Path.Combine(FindRepoRoot(), "database", "starter");
        var service = new GameDatabaseService(starterRoot);

        var games = service.LoadAll();

        Assert.NotEmpty(games);
        Assert.All(games, g => Assert.NotEmpty(g.TrackedValues));
        Assert.All(games, g => Assert.NotEqual(Core.Models.ConsoleType.Unknown, g.Console));
    }

    [Fact]
    public void AllStarterTemplates_TrackedValueAddresses_FallWithinTheirConsolesWramWindow()
    {
        string starterRoot = Path.Combine(FindRepoRoot(), "database", "starter");
        var service = new GameDatabaseService(starterRoot);

        foreach (var game in service.LoadAll())
        {
            var space = Core.Addressing.ConsoleAddressSpace.For(game.Console);
            foreach (var tracked in game.TrackedValues)
            {
                Assert.True(
                    space.ContainsConsoleAddress(tracked.ConsoleAddress),
                    $"{game.Title}/{tracked.Label}: 0x{tracked.ConsoleAddress:X} is outside {game.Console} WRAM.");
            }
        }
    }
}
