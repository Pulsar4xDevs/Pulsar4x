using System;
using System.IO;
using System.Linq;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.SDL2UI;
using Pulsar4X.SDL2UI.ModFileEditing;

namespace Pulsar4X.Client.Interface.Menus;

public class LoadGame : PulsarGuiWindow
{
    private string _filePath = Path.Combine(PulsarMainWindow.GetAppDataPath(), PulsarMainWindow.SavesPath);
    private string _fileName = "savegame";

    private LoadGame() {}
    
    internal static LoadGame GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(LoadGame)))
        {
            return new LoadGame();
        }
        return (LoadGame)_uiState.LoadedWindows[typeof(LoadGame)];
    }

    internal void LoadLatest()
    {
        var files = Directory.EnumerateFiles(_filePath);
        DateTime date = DateTime.UnixEpoch;
        string? fileToLoad = null;
        foreach (var file in files)
        {
            FileInfo fi = new FileInfo(file);
            if (fi.LastWriteTime > date)
            {
                fileToLoad = file;
                date = fi.LastWriteTime;
            }
        }
        if(!string.IsNullOrEmpty(fileToLoad))
            LoadFile(Path.Combine(fileToLoad, fileToLoad));
            
    }

    internal void LoadFile(string filenamepath)
    {
        string contents = File.ReadAllText(filenamepath);
        var loadedGame = Game.Load(contents);

        _uiState.Game = loadedGame;

        // TODO: need to figure out a way to properly handle this
        (int id, Entity faction) = loadedGame.Factions.First(f => f.Value.GetOwnersName().Equals("UEF"));
        _uiState.SetFaction(faction, true);
        _uiState.SetActiveSystem(faction.GetDataBlob<FactionInfoDB>().KnownSystems[0]);
            
        DebugWindow.GetInstance().SetGameEvents();
        //we initialize window instances so that they get always displayed and automatically open after new game is created.
        TimeControl.GetInstance().SetActive();
        ToolBarWindow.GetInstance().SetActive();
        Selector.GetInstance().SetActive();
    }

    internal override void Display()
    {
        if (IsActive && FileDialog.DisplayLoad(ref _filePath, ref _fileName, ref IsActive))
        {
            if (String.IsNullOrEmpty(_fileName) || String.IsNullOrEmpty(_filePath))
            {
                IsActive = false;
                return;
            }
            LoadFile(Path.Combine(_filePath, _fileName));
            
            IsActive = false;
        }
    }
}