using System;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Widgets;

public class ResultModal : PulsarGuiWindow
{
    private byte[]? _inputBuffer = null;
    uint _bufferMaxSize = 64;
    private Action<Action<string>>? _customRenderer = null;

    internal ResultModal()
    {
        Array.Resize(ref _inputBuffer, checked((int)_bufferMaxSize)); //Resize the text buffer
        _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;
    }

    internal static ResultModal GetInstance()
    {
        if (!_uiState.LoadedWindows.ContainsKey(typeof(ResultModal)))
        {
            return new ResultModal();
        }
        return (ResultModal)_uiState.LoadedWindows[typeof(ResultModal)];
    }

    internal override void Display()
    {
        if(!IsActive) return;
    }

    // Generic modal with custom renderer
    public void Display(string title, Action? onOk, Action? onCancel, Action contentRenderer, string okLabel = "Ok", string cancelLabel = "Cancel")
    {
        string fullTitle = title + $"###{title}-display-modal";

        if(!IsActive)
        {
            ImGui.OpenPopup(fullTitle);
            IsActive = true;
        }

        if (ImGui.BeginPopupModal(fullTitle, _flags))
        {
            // Call the content renderer and provide a callback for submitting the result
            contentRenderer?.Invoke();

            if (onOk != null && ImGui.Button(okLabel))
            {
                ImGui.CloseCurrentPopup();
                IsActive = false;
                onOk?.Invoke();
            }

            ImGui.SameLine();

            if (onCancel != null && ImGui.Button(cancelLabel))
            {
                ImGui.CloseCurrentPopup();
                IsActive = false;
                onCancel?.Invoke();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Display a modal where the content renderer handles all buttons.
    /// The closeModal action is passed to the content renderer to allow custom button placement.
    /// </summary>
    public void DisplayCustomButtons(string title, Action? onClose, Action<Action> contentRenderer)
    {
        string fullTitle = title + $"###{title}-display-modal";

        if(!IsActive)
        {
            ImGui.OpenPopup(fullTitle);
            IsActive = true;
        }

        if (ImGui.BeginPopupModal(fullTitle, _flags))
        {
            // Create the close action that the content renderer can use
            Action closeModal = () =>
            {
                ImGui.CloseCurrentPopup();
                IsActive = false;
                onClose?.Invoke();
            };

            // Call the content renderer with the close action
            contentRenderer?.Invoke(closeModal);

            ImGui.EndPopup();
        }
    }
}