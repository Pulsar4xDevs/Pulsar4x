using System;
using ImGuiNET;

namespace Pulsar4X.SDL2UI;

public class TextModal : PulsarGuiWindow
{
    private byte[]? _inputBuffer = null;
    uint _bufferMaxSize = 64;

    internal TextModal()
    {
        Array.Resize(ref _inputBuffer, checked((int)_bufferMaxSize));//Resize the text buffer
        _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;
    }

    internal static TextModal GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(TextModal)))
        {
            return new TextModal();
        }

        return (TextModal)_uiState.LoadedWindows[typeof(TextModal)];
    }

    internal override void Display()
    {
        if(!IsActive) return;
    }

    public void DisplayModal(string title, Action<string> onOk, Action onCancel)
    {
        string fullTitle = title + $"###{title}-display-modal";

        if(!IsActive)
        {
            ImGui.OpenPopup(fullTitle);
            IsActive = true;
        }
        if (ImGui.BeginPopupModal(fullTitle, ref IsActive, _flags))
        {
            ImGui.InputText("##modal-name", _inputBuffer, _bufferMaxSize, ImGuiInputTextFlags.AutoSelectAll);//Gets the text from the user and stores it into the buffer

            ImGui.SameLine();
            if (ImGui.Button("Ok"))//Gives the user the option to set the name
            {
                if(_inputBuffer != null)
                {
                    ImGui.CloseCurrentPopup();
                    IsActive = false;

                    int actualLength = Array.IndexOf(_inputBuffer, (byte)0);
                    if (actualLength == -1)
                    {
                        // No null terminator found; use the entire buffer
                        actualLength = _inputBuffer.Length;
                    }
                    onOk?.Invoke(System.Text.Encoding.UTF8.GetString(_inputBuffer, 0, actualLength));
                }
            }
            ImGui.SameLine();
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
                IsActive = false;
                onCancel?.Invoke();
            }
            ImGui.EndPopup();
        }
    }
}