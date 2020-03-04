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
            _flags = ImGuiWindowFlags.AlwaysAutoResize;
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
                    //TODO: Move this to settings
                    uint umaxnamesize = 64;

                    Array.Resize(ref nameInputBuffer, checked((int)umaxnamesize));//Resize the text buffer
                    ImGui.InputText("##name", nameInputBuffer, umaxnamesize);//Gets the text from the user and stores it into the buffer

                    ImGui.SameLine();
                    if (ImGui.SmallButton("Set"))//Gives the user the option to set the name
                    {
                        if(nameInputBuffer[0] != 0){//If the user has not entered an empty name
                        
                            RenameCommand.CreateRenameCommand(_state.Game, _state.Faction, _entityState.Entity, nameString);
                            _entityState.Name = nameString;//Rename the object
                            _entityState.NameIcon.NameString = nameString;//Rename the name of the object on the map
                            IsActive = false;//Close the window
                        }

                        
                    }
                }
                ImGui.End();
            }
        }
    }
}
