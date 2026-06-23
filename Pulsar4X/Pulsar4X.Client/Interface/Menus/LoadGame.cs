using System;
using System.IO;
using Pulsar4X.Client.Interface.Widgets;

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
        var activation = _uiState.Lifecycle?.LoadGame(filenamepath);
        if (activation == null) return;

        _uiState.ActivateGameUI(activation);
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
