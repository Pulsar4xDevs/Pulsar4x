using System;
using System.Diagnostics;
using System.IO;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client.Interface.Menus;

public class LoadGame : UniquePulsarGuiWindow<LoadGame>
{
    private string _filePath = Path.Combine(PulsarMainWindow.GetAppDataPath(), PulsarMainWindow.SavesPath);
    private string _fileName = "savegame";
    private string? _error;

    private LoadGame() {}

    internal static LoadGame GetInstance()
    {
        if(_uiState.TryGetUniqueWindow<LoadGame>(out var window))
        {
            return window;
        }
        return _uiState.AddUniqueWindow(new LoadGame());
    }

    internal bool LoadLatest()
    {
        try
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
            if (string.IsNullOrEmpty(fileToLoad))
            {
                _error = "No save was found.";
                return false;
            }

            return LoadFile(fileToLoad);
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            _error = ex.ToString();
            return false;
        }
    }

    internal bool LoadFile(string filenamepath)
    {
        try
        {
            var activation = _uiState.Lifecycle?.LoadGame(filenamepath);
            if (activation == null)
            {
                _error = "Could not load that save.";
                return false;
            }

            _uiState.ActivateGameUI(activation);
            _error = null;
            return true;
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            _error = ex.ToString();
            return false;
        }
    }

    internal override void Display()
    {
        if (!IsActive)
            return;

        bool accepted = FileDialog.DisplayLoad(ref _filePath, ref _fileName, ref IsActive, _error);
        if (!accepted)
        {
            if (!IsActive)
                _error = null;
            return;
        }

        if (String.IsNullOrEmpty(_fileName) || String.IsNullOrEmpty(_filePath))
        {
            IsActive = false;
            _error = null;
            return;
        }

        if (LoadFile(Path.Combine(_filePath, _fileName)))
            IsActive = false;
        else
        {
            IsActive = true;
            Pulsar4X.Client.MainMenuItems.GetInstance().SetActive(true);
        }
    }
}
