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

        private void reset(EntityState entity){
            _entityState = entity;
            nameInputBuffer = System.Text.Encoding.UTF8.GetBytes(_entityState.Name);
        }

        public RenameWindow(EntityState entity)
        {
            reset(entity);
        }
        internal static RenameWindow GetInstance(EntityState entity)
        {
            if (!_state.LoadedWindows.ContainsKey(typeof(RenameWindow)))
            {
                return new RenameWindow(entity);
            }
            var retval = (RenameWindow)_state.LoadedWindows[typeof(RenameWindow)];
            retval.reset(entity);
            return retval;
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
                        if(nameInputBuffer[0] != 0){
                        
                            RenameCommand.CreateRenameCommand(_state.Game, _state.Faction, _entityState.Entity, nameString);
                            _entityState.Name = nameString;
                            _entityState.NameIcon.NameString = nameString;
                            IsActive = false;
                        }

                        
                        
                        
                    }
                    ImGui.Text(nameString.Length.ToString());
                }
                ImGui.End();
            }
        }
    }
}
