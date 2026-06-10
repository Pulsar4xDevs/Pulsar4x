using System;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Client
{
    public class RenameWindow : PulsarGuiWindow
    {
        private int _targetEntityId = -1;
        private byte[]? _nameInputBuffer;
        string NameString
        {
            get
            {
                if(_nameInputBuffer == null)
                    return "";
                return System.Text.Encoding.UTF8.GetString(_nameInputBuffer).TrimEnd('\0');
            }
        }
        private bool _setFocus = true;

        private RenameWindow()
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;
        }

        /// <summary>Open the rename dialog for an entity by id; the rename is submitted as an API
        /// command, so callers don't need a live engine entity.</summary>
        public void SetTarget(int entityId, string currentName)
        {
            _targetEntityId = entityId;
            _nameInputBuffer = System.Text.Encoding.UTF8.GetBytes(currentName);
            IsActive = true;
            _setFocus = true;
        }

        /// <summary>Engine-entity convenience for not-yet-ported callers.</summary>
        public void SetEntity(Entity entity)
        {
            if(_uiState.Faction == null)
                throw new NullReferenceException("_uiState.Faction cannot be null");

            SetTarget(entity.Id, entity.GetName(_uiState.Faction.Id));
        }

        internal static RenameWindow GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(RenameWindow)))
            {
                return new RenameWindow();
            }
            return (RenameWindow)_uiState.LoadedWindows[typeof(RenameWindow)];
        }

        internal override void Display()
        {
            if(IsActive) ImGui.OpenPopup("Rename");

            if (ImGui.BeginPopupModal("Rename", ref IsActive, _flags))
            {
                //TODO: Move this to settings
                uint umaxnamesize = 64;

                Array.Resize(ref _nameInputBuffer, checked((int)umaxnamesize));//Resize the text buffer

                if(_setFocus)
                {
                    ImGui.SetKeyboardFocusHere();
                    _setFocus = false;
                }

                ImGui.InputText("##name", _nameInputBuffer, umaxnamesize, ImGuiInputTextFlags.AutoSelectAll);//Gets the text from the user and stores it into the buffer

                ImGui.SameLine();
                if (ImGui.SmallButton("Save"))//Gives the user the option to set the name
                {
                    //If the user has not entered an empty name
                    if(_nameInputBuffer[0] != 0 && _targetEntityId != -1)
                    {
                        _uiState.GameClient?.SubmitCommandAsync(
                            new Pulsar4X.Api.RenameCommand(_targetEntityId, NameString));
                        ImGui.CloseCurrentPopup();
                        IsActive = false;
                    }
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                    IsActive = false;
                }
                ImGui.EndPopup();
            }
        }
    }
}
