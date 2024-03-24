using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
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
                if(ImGui.CollapsingHeader("Systems", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    // FIXME: this can be done once and updated only when KnownSystems changes
                    var knownSystems = _uiState.Faction.GetDataBlob<FactionInfoDB>().KnownSystems;
                    var filteredAndSortedSystems = _uiState.Game.Systems
                                                        .Where(s => knownSystems.Contains(s.Guid))
                                                        .OrderBy(s => s.NameDB.OwnersName)
                                                        .ToList();

                    foreach(var system in filteredAndSortedSystems)
                    {
                        if(ImGui.Selectable(system.NameDB.OwnersName, _uiState.SelectedStarSysGuid.Equals(system.Guid)))
                        {
                            _uiState.SelectedSysMapRender.OnSelectedSystemChange(system);
                        }
                    }
                }
                if(ImGui.CollapsingHeader("Colonies", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var colonies = _uiState.Faction.GetDataBlob<FactionInfoDB>().Colonies;
                    foreach(var colony in colonies)
                    {
                        bool visible = EconomicsWindow.GetInstance().GetActive() && EconomicsWindow.GetInstance().SelectedEntity?.Entity.Id == colony.Id;
                        if(ImGui.Selectable(colony.GetName(_uiState.Faction.Id), visible))
                        {
                            if(_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid) && _uiState.StarSystemStates[_uiState.SelectedStarSysGuid].EntityStatesColonies.ContainsKey(colony.Id))
                            {
                                EconomicsWindow.GetInstance().SetActive(true);
                                EconomicsWindow.GetInstance().SelectEntity(_uiState.StarSystemStates[_uiState.SelectedStarSysGuid].EntityStatesColonies[colony.Id]);
                            }
                        }
                    }
                }
                if(ImGui.CollapsingHeader("Fleets", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    var fleets = _uiState.Faction.GetDataBlob<FleetDB>().RootDB?.Children ?? new SafeList<Entity>();

                    foreach(var fleet in fleets)
                    {
                        bool visible = FleetWindow.GetInstance().GetActive() && FleetWindow.GetInstance().SelectedFleet?.Id == fleet.Id;
                        if(ImGui.Selectable(fleet.GetName(_uiState.Faction.Id), visible))
                        {
                            FleetWindow.GetInstance().SetActive(true);
                            FleetWindow.GetInstance().SelectFleet(fleet);
                        }
                    }
                }
                ImGui.End();
            }
        }
    }
}