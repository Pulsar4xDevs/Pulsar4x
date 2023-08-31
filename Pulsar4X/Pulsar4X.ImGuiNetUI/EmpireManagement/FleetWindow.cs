using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using ImGuiSDL2CS;
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
                    if(ImGui.TreeNodeEx(name))
                    {
                        ImGui.TreePop();
                    }
                    if(ImGui.TreeNode(name + "###" + fleet.Guid))
                    {
                        // Begin drag source
                        if(ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover))
                        {
                            var (ptr, length) = ImGuiSDL2CSHelper.GuidToIntPtr(fleet.Guid);
                            ImGui.SetDragDropPayload("FLEET", ptr, length);
                            ImGui.Text(name);
                            ImGui.EndDragDropSource();
                            Marshal.FreeHGlobal(ptr);
                        }

                        // Begin Drag Target
                        if (ImGui.BeginDragDropTarget())
                        {
                            //var payload = ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                            //if(payload.IsDelivery())
                            //{
                            //    var draggedFleetGuid = payload.Data; // convert to appropriate type
                                // TODO: Handle the logic to nest the dragged fleet under the current fleet.
                            //}
                            ImGui.EndDragDropTarget();
                        }
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
            }
        }
    }
}