using System;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.SDL2UI
{
    public class RenameWindow : PulsarGuiWindow
    {
        private Entity? _selectedEntity;
        private byte[]? _nameInputBuffer;
        string NameString
        {
            get
            {
                if(_nameInputBuffer == null)
                    return "";
                return System.Text.Encoding.UTF8.GetString(_nameInputBuffer); 
            }
        }
        private bool _setFocus = true;

        private RenameWindow()
        {
            _flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse;
        }

        public void SetEntity(Entity entity)
        {
            _selectedEntity = entity;
            _nameInputBuffer = System.Text.Encoding.UTF8.GetBytes(entity.GetName(_uiState.Faction.Id));
            IsActive = true;
            _setFocus = true;
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
                    if(_nameInputBuffer[0] != 0 && _selectedEntity != null)//If the user has not entered an empty name
                    {
                        RenameCommand.CreateRenameCommand(_uiState.Game, _uiState.Faction, _selectedEntity, NameString);
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
