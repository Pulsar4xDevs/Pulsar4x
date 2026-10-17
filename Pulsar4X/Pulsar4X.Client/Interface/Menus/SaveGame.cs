using System;
using System.Diagnostics;
using System.IO;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client.Interface.Menus;

public class SaveGame : UniquePulsarGuiWindow<SaveGame>
{
    private string _filePath = Path.Combine(PulsarMainWindow.GetAppDataPath(), PulsarMainWindow.SavesPath);
    private string _fileName = "savegame.sav";
    private string? _error;

    private SaveGame() {}

    internal static SaveGame GetInstance()
    {
        if(_uiState.TryGetUniqueWindow<SaveGame>(out var window))
        {
            return window;
        }
        return _uiState.AddUniqueWindow(new SaveGame());
    }

    internal override void Display()
    {
        if (!IsActive)
            return;

        bool accepted = FileDialog.DisplaySave(ref _filePath, ref _fileName, ref IsActive, _error);
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

        try
        {
            _uiState.Lifecycle?.SaveGame(Path.Combine(_filePath, _fileName));
            _error = null;
        }
        catch (Exception ex) when (!Debugger.IsAttached)
        {
            _error = ex.ToString();
            IsActive = true;
        }
    }

    public void UpdateSaveName(string name)
    {
        _fileName = name + ".sav";
    }
}
