using System.Collections.ObjectModel;
using RomMemoryScanner.Core.Database;
using RomMemoryScanner.Core.Models;

namespace RomMemoryScanner.App.ViewModels;

/// <summary>Address database browser/editor (FR-7.1 view 5, FR-3.1-3.4).</summary>
public sealed class DatabaseBrowserViewModel : ViewModelBase
{
    private readonly GameDatabaseService _databaseService;
    private readonly string _databaseRoot;
    private Game? _selectedGame;
    private string? _statusMessage;

    public DatabaseBrowserViewModel(GameDatabaseService databaseService, string databaseRoot)
    {
        _databaseService = databaseService;
        _databaseRoot = databaseRoot;
        Games = new ObservableCollection<Game>();
        ReloadCommand = new RelayCommand(_ => Reload());
        Reload();
    }

    public ObservableCollection<Game> Games { get; }

    public Game? SelectedGame
    {
        get => _selectedGame;
        set => SetField(ref _selectedGame, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public RelayCommand ReloadCommand { get; }

    public void Reload()
    {
        Games.Clear();
        foreach (Game game in _databaseService.LoadAll())
        {
            Games.Add(game);
        }

        StatusMessage = $"Loaded {Games.Count} game(s) from {_databaseRoot}.";
    }

    /// <summary>FR-3.2: import a user-supplied or shared address map JSON file into the in-memory list.</summary>
    public void Import(string filePath)
    {
        try
        {
            Game imported = GameDatabaseService.Import(filePath);
            Games.Add(imported);
            SelectedGame = imported;
            StatusMessage = $"Imported '{imported.Title}'. Use Save to add it to the local database.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
    }

    /// <summary>FR-3.2: export the selected game's address map for sharing/backup.</summary>
    public void Export(Game game, string filePath)
    {
        try
        {
            GameDatabaseService.Export(game, filePath);
            StatusMessage = $"Exported '{game.Title}' to {filePath}.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    /// <summary>Persists an imported/edited entry into the local database folder, under its console.</summary>
    public void SaveToLocalDatabase(Game game)
    {
        string safeFileName = string.Concat(game.Title.Split(Path.GetInvalidFileNameChars())).Replace(' ', '-').ToLowerInvariant();
        string path = Path.Combine(_databaseRoot, game.Console.ToString().ToLowerInvariant(), $"{safeFileName}.json");
        GameDatabaseService.SaveToFile(game, path);
        StatusMessage = $"Saved '{game.Title}' to {path}.";
    }
}
