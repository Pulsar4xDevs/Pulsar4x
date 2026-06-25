using System;
using System.IO;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client.Interface.Menus;

public class SaveGame : UniquePulsarGuiWindow<SaveGame>
{
    private string _filePath = Path.Combine(PulsarMainWindow.GetAppDataPath(), PulsarMainWindow.SavesPath);
    private string _fileName = "savegame.sav";

    private SaveGame() {}

    internal static SaveGame GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(SaveGame)))
        {
            return new SaveGame();
        }
        return (SaveGame)_uiState.LoadedWindows[typeof(SaveGame)];
    }

    internal override void Display()
    {
        if (IsActive && FileDialog.DisplaySave(ref _filePath, ref _fileName, ref IsActive))
        {
            if (String.IsNullOrEmpty(_fileName) || String.IsNullOrEmpty(_filePath))
            {
                IsActive = false;
                return;
            }

            _uiState.Lifecycle?.SaveGame(Path.Combine(_filePath, _fileName));
        }
    }

    public void UpdateSaveName(string name)
    {
        _fileName = name + ".sav";
    }
}
