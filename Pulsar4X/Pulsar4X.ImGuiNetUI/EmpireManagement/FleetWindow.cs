using System;
using System.Numerics;
using Antlr.Runtime;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class FleetWindow : PulsarGuiWindow
    {
        private FactionInfoDB factionInfoDB;
        private Guid factionID;

        private FleetWindow()
        {
            factionInfoDB = _uiState.Faction.GetDataBlob<FactionInfoDB>();
            factionID = _uiState.Faction.Guid;
        }
        internal static FleetWindow GetInstance()
        {
            FleetWindow thisitem;
            if (!_uiState.LoadedWindows.ContainsKey(typeof(FleetWindow)))
            {
                thisitem = new FleetWindow();
            }
            thisitem = (FleetWindow)_uiState.LoadedWindows[typeof(FleetWindow)];

            return thisitem;
        }
        internal override void Display()
        {
            if(!IsActive) return;

            if(ImGui.Begin("Fleet Management", ref IsActive, _flags))
            {
                DisplayFleetList();
                ImGui.End();
            }
        }

        private void DisplayFleetList()
        {
            Vector2 windowContentSize = ImGui.GetContentRegionAvail();
            if(ImGui.BeginChild("FleetListSelection", new Vector2(Styles.LeftColumnWidth, windowContentSize.Y - 24f), true))
            {
                DisplayHelpers.Header("Fleets", "Select a fleet to manage it.");

                foreach(var fleet in factionInfoDB.Fleets.ToArray())
                {
                    string name = fleet.GetDataBlob<NameDB>().GetName(factionID);
                    if(ImGui.TreeNode(name + "###" + fleet.Guid))
                    {
                        ImGui.TreePop();
                    }
                    if(ImGui.BeginPopupContextItem())
                    {
                        if(ImGui.MenuItem("Disband###delete-" + fleet.Guid))
                        {
                            // FIXME: needs some checking to make sure the fleet has no ships
                            factionInfoDB.Fleets.Remove(fleet);
                        }
                        ImGui.EndPopup();
                    }
                }
                ImGui.EndChild();
            }

            if(ImGui.Button("Create New Fleet", new Vector2(204f, 0f)))
            {
                string name = "auto-gen names pls";
                Entity fleet = FleetFactory.Create(_uiState.Game.GlobalManager, factionID, name);

                factionInfoDB.Fleets.Add(fleet);

                // string originalName = "auto-gen names pls", name = originalName;
                // int counter = 1;
                // while(_factionInfoDB.ShipDesigns.Values.Any(d => d.Name.Equals(name)))
                // {
                //     name = originalName + " " + counter.ToString();
                //     counter++;
                // }
                // SelectedDesignName = ImGuiSDL2CSHelper.BytesFromString(name);
                // SelectedComponents = new List<(ComponentDesign design, int count)>();
                // GenImage();
                // RefreshArmor();
                // DesignChanged = true;

                // ShipDesign design = new(_factionInfoDB, name, SelectedComponents, (_armor, _armorThickness))
                // {
                //     IsValid = false
                // };
                // RefreshExistingClasses();
                // SelectedExistingDesignID = design.ID;
            }
        }
    }
}