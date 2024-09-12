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
                var systemViewPreferences = SystemViewPreferences.GetInstance();
                int viewIndex = systemViewPreferences.GetViewIndex("map");

                if(ImGui.Combo("###view-selector", ref viewIndex, systemViewPreferences.ViewNames, systemViewPreferences.ViewNames.Length))
                {
                    systemViewPreferences.SetViewIndex("map", viewIndex);
                    _uiState.SelectedMapView = systemViewPreferences.GetViewByIndex(viewIndex);
                    ImGui.EndCombo();
                }

                if(ImGui.CollapsingHeader("Systems", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    // FIXME: this can be done once and updated only when KnownSystems changes
                    var knownSystems = _uiState.Faction.GetDataBlob<FactionInfoDB>().KnownSystems;
                    var filteredAndSortedSystems = _uiState.Game.Systems
                                                        .Where(s => knownSystems.Contains(s.ID))
                                                        .OrderBy(s => s.NameDB.OwnersName)
                                                        .ToList();

                    foreach(var system in filteredAndSortedSystems)
                    {
                        if(ImGui.Selectable(system.NameDB.OwnersName, _uiState.SelectedStarSysGuid.Equals(system.ID)))
                        {
                            _uiState.SetActiveSystem(system.ID);
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
                        string display = fleet.GetName(_uiState.Faction.Id);
                        if(ImGui.Selectable(display, visible))
                        {
                            FleetWindow.GetInstance().SetActive(true);
                            FleetWindow.GetInstance().SelectFleet(fleet);
                        }

                        if (ImGui.IsItemHovered())
                        {
                            void Callback()
                            {
                                if(fleet.TryGetDatablob<OrderableDB>(out var orderableDb)
                                && orderableDb.ActionList.Count > 0)
                                {
                                    ImGui.Text("Orders:");
                                    for(int i = 0; i < orderableDb.ActionList.Count; i++)
                                    {
                                        ImGui.Text(orderableDb.ActionList[i].Name);
                                    }
                                }
                                else
                                {
                                    ImGui.Text("No orders");
                                }
                            }

                            var flagshipID = fleet.GetDataBlob<FleetDB>().FlagShipID;
                            if(fleet.Manager?.TryGetEntityById(flagshipID, out var flagship) ?? false)
                            {
                                var positionDB = flagship.GetDataBlob<PositionDB>();
                                DisplayHelpers.DescriptiveTooltip(display, positionDB.Parent?.GetName(_uiState.Faction.Id) ?? "Unknown", "", Callback);
                            }
                        }
                    }
                }
                ImGui.End();
            }
        }
    }
}