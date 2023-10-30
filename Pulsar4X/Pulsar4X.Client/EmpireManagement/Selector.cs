using System.Numerics;
using ImGuiNET;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
{
    public class Selector : PulsarGuiWindow
    {
        //constructs the toolbar with the given buttons
        private Selector()
        {
            _flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDocking;
        }

        internal static Selector GetInstance()
        {
            if (!PulsarGuiWindow._uiState.LoadedWindows.ContainsKey(typeof(Selector)))
            {
                return new Selector();
            }

            return (Selector)_uiState.LoadedWindows[typeof(Selector)];
        }

        internal override void Display()
        {
            if(!IsActive) return;

            ImGui.SetNextWindowSize(new Vector2(256, 0));
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetMainViewport().WorkSize.X - 256, 0));
            ImGui.SetNextWindowBgAlpha(0);
            if(ImGui.Begin("###selector", _flags))
            {
                if(ImGui.CollapsingHeader("Colonies", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var colonies = _uiState.Faction.GetDataBlob<FactionInfoDB>().Colonies;
                    foreach(var colony in colonies)
                    {
                        if(ImGui.Selectable(colony.GetName(_uiState.Faction.Id)))
                        {
                            _uiState.EntityClicked(colony.Id, colony.Manager.ManagerGuid, MouseButtons.Primary);
                        }
                    }
                }
                if(ImGui.CollapsingHeader("Fleets", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var fleets = _uiState.Faction.GetDataBlob<FleetDB>().RootDB.Children;
                    foreach(var fleet in fleets)
                    {
                        if(ImGui.Selectable(fleet.GetName(_uiState.Faction.Id)))
                        {
                            _uiState.EntityClicked(fleet.Id, fleet.Manager.ManagerGuid, MouseButtons.Primary);
                        }
                    }
                }
                ImGui.End();
            }
        }
    }
}