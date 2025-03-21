using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Ships;

namespace Pulsar4X.Client
{
    public class Selector : PulsarGuiWindow
    {
        private List<string> _knownSystems = new ();
        private List<StarSystem> _filteredAndSortedSystems = new ();

        //constructs the toolbar with the given buttons
        private Selector()
        {
            _flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDocking;

            _uiState.OnStarSystemAdded += SystemAdded;
        }

        internal static Selector GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(Selector)))
            {
                return new Selector();
            }

            return (Selector)_uiState.LoadedWindows[typeof(Selector)];
        }

        internal override void Display()
        {
            if(!IsActive || _uiState.Faction == null) return;
            if(_knownSystems.Count == 0) RefreshSystems();

            ImGui.SetNextWindowSize(new Vector2(256, 0));
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetMainViewport().WorkSize.X - 256, 0));
            ImGui.SetNextWindowBgAlpha(0);
            if(Window.Begin("###selector", _flags))
            {
                // TODO: re-implement this somewhere
                // SystemViewPreferences.GetInstance().DisplayCombo("map", selectedIndex =>
                // {
                //     _uiState.SelectedMapView = SystemViewPreferences.GetInstance().GetViewByIndex(selectedIndex);
                // });

                DisplayHelpers.Header($"{_uiState.Faction.GetFactionName()} [{_uiState.Faction.GetFactionAbbreviation()}]");
                DisplaySystems();
                DisplayColonies();
                DisplayFleets();
            }
            Window.End();
        }

        private void RefreshSystems()
        {
            _knownSystems = _uiState.Faction?.GetDataBlob<FactionInfoDB>().KnownSystems ?? new ();
            FilterAndSortSystems();
        }

        private void FilterAndSortSystems()
        {
            _filteredAndSortedSystems = _uiState.Game?.Systems
                                            .Where(s => _knownSystems.Contains(s.ID))
                                            .OrderBy(s => s.NameDB.OwnersName)
                                            .ToList() ?? new ();
        }

        private void SystemAdded(GlobalUIState state, string systemId)
        {
            if(!_knownSystems.Contains(systemId))
                _knownSystems.Add(systemId);

            FilterAndSortSystems();
        }

        private void DisplaySystems()
        {
            if (ImGui.CollapsingHeader("Systems", ImGuiTreeNodeFlags.DefaultOpen))
            {
                foreach (var system in _filteredAndSortedSystems)
                {
                    if (ImGui.Selectable(system.NameDB.OwnersName, _uiState.SelectedStarSystemId.Equals(system.ID)))
                    {
                        _uiState.SetActiveSystem(system.ID);
                    }
                }
            }
        }

        private static void DisplayColonies()
        {
            if (ImGui.CollapsingHeader("Colonies", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if(_uiState.Faction == null) return;

                var colonies = _uiState.Faction.GetDataBlob<FactionInfoDB>().Colonies;

                foreach (var colony in colonies)
                {
                    bool visible = ColonyManagementWindow.GetInstance().GetActive() && ColonyManagementWindow.GetInstance().SelectedEntity?.Entity.Id == colony.Id;
                    if (ImGui.Selectable(colony.GetName(_uiState.Faction.Id), visible))
                    {
                        if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSystemId) && _uiState.StarSystemStates[_uiState.SelectedStarSystemId].EntityStatesColonies.ContainsKey(colony.Id))
                        {
                            ColonyManagementWindow.GetInstance().SetActive(true);
                            ColonyManagementWindow.GetInstance().SelectEntity(_uiState.StarSystemStates[_uiState.SelectedStarSystemId].EntityStatesColonies[colony.Id]);
                        }
                    }
                }
            }
        }

        private static void DisplayFleets()
        {
            if (ImGui.CollapsingHeader("Fleets", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if(_uiState.Faction == null) return;

                var fleets = _uiState.Faction.GetDataBlob<FleetDB>().RootDB?.Children ?? new SafeList<Entity>();

                foreach (var fleet in fleets)
                {
                    // Check if the entity is actually a ship
                    if (fleet.HasDataBlob<ShipInfoDB>())
                        continue;

                    bool visible = FleetWindow.GetInstance().GetActive() && FleetWindow.GetInstance().SelectedFleet?.Id == fleet.Id;
                    string display = fleet.GetName(_uiState.Faction.Id);
                    if (ImGui.Selectable(display, visible))
                    {
                        FleetWindow.GetInstance().SelectFleet(fleet);
                        FleetWindow.GetInstance().SetActive(true);
                    }

                    if (ImGui.IsItemHovered())
                    {
                        void Callback()
                        {
                            if (fleet.TryGetDatablob<OrderableDB>(out var orderableDb)
                            && orderableDb.ActionList.Count > 0)
                            {
                                ImGui.Text("Orders:");
                                for (int i = 0; i < orderableDb.ActionList.Count; i++)
                                {
                                    ImGui.Text(orderableDb.ActionList[i].Name);
                                }
                            }
                            else
                            {
                                ImGui.Text("No orders");
                            }
                        }

                        fleet.TryGetDatablob<FleetDB>(out var fleetDB);
                        var flagshipID = fleetDB?.FlagShipID ?? -9999;
                        if (fleet.Manager?.TryGetEntityById(flagshipID, out var flagship) ?? false)
                        {
                            var positionDB = flagship.GetDataBlob<PositionDB>();
                            DisplayHelpers.DescriptiveTooltip(display, positionDB.Parent?.GetName(_uiState.Faction.Id) ?? "Unknown", "", Callback);
                        }
                    }
                }
            }
        }
    }
}