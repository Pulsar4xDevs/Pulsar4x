using System;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class RenameWindow : PulsarGuiWindow
    {
        EntityState _entityState;
        byte[] nameInputBuffer; 
        string nameString { get { return System.Text.Encoding.UTF8.GetString(nameInputBuffer); } }

        public RenameWindow(EntityState entity)
        {
            _entityState = entity;
            nameInputBuffer = System.Text.Encoding.UTF8.GetBytes(_entityState.Name);
        }
        internal static RenameWindow GetInstance(EntityState entity)
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(RenameWindow)))
            {
                return new RenameWindow(entity);
            }
            return (RenameWindow)_state.LoadedWindows[typeof(RenameWindow)];
        }

        internal override void Display()
        {
            if (IsActive)
            {
                if (ImGui.Begin("Rename", ref IsActive, _flags))
                {
                    ImGui.InputText("##name", nameInputBuffer, 16);
                    ImGui.SameLine();
                    if (ImGui.SmallButton("Set"))
                    {
                        RenameCommand.CreateRenameCommand(_state.Game, _state.Faction, _entityState.Entity, nameString);
                        _entityState.Name = nameString;
                        _entityState.NameIcon.NameString = nameString;
                        IsActive = false;
                    }
                }
                ImGui.End();
            }
        }
    }
}
