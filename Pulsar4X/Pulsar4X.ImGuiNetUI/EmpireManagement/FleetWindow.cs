using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public class FleetWindow : PulsarGuiWindow
    {
        private FactionInfoDB factionInfoDB;
        private Guid factionID;
        private IntPtr dragPayload;
        private Entity dragEntity = Entity.InvalidEntity;
        int nameCounter = 1;

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

                // We need a drop target here so nested items can be un-nested to the root of the tree
                if(ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                    if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragEntity != Entity.InvalidEntity)
                    {
                        // Remove the dragEntity from the parent tree or the facction Fleets
                        var sourceFleetInfo = dragEntity.GetDataBlob<FleetDB>();

                        // Check if nested
                        if(sourceFleetInfo.Root != dragEntity)
                        {
                            sourceFleetInfo.ParentDB.Children.Remove(dragEntity);
                        }

                        // Drop the dragEntity
                        if(!factionInfoDB.Fleets.Contains(dragEntity))
                        {
                            factionInfoDB.Fleets.Add(dragEntity);
                        }
                        dragEntity = Entity.InvalidEntity;
                    }
                    ImGui.EndDragDropTarget();
                }

                foreach(var fleet in factionInfoDB.Fleets.ToArray())
                {
                    DisplayFleetItem(fleet);
                }
                ImGui.EndChild();
            }

            if(ImGui.Button("Create New Fleet", new Vector2(204f, 0f)))
            {
                string name = "auto-gen names pls " + nameCounter++;
                Entity fleet = FleetFactory.Create(_uiState.Game.GlobalManager, factionID, name);

                factionInfoDB.Fleets.Add(fleet);
            }
        }

        private void DisplayFleetItem(Entity fleet)
        {
            ImGui.PushID(fleet.Guid.ToString());
            string name = fleet.GetDataBlob<NameDB>().GetName(factionID);
            var fleetInfo = fleet.GetDataBlob<FleetDB>();
            var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.FramePadding;

            if(fleetInfo.Children.Count == 0)
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            bool isTreeOpen = ImGui.TreeNodeEx(name, flags);

            if(isTreeOpen)
            {
                DisplayContextMenu(fleet);
                DisplayDropSource(fleet, name);
                DisplayDropTarget(fleet);
                foreach(var child in fleetInfo.Children.ToArray())
                {
                    DisplayFleetItem(child);
                }
                ImGui.TreePop();
            }

            if(!isTreeOpen)
            {
                DisplayContextMenu(fleet);
                DisplayDropSource(fleet, name);
                DisplayDropTarget(fleet);
            }
            ImGui.PopID();
        }

        private void DisplayContextMenu(Entity fleet)
        {
            if(ImGui.BeginPopupContextItem())
            {
                if(ImGui.MenuItem("Disband###delete-" + fleet.Guid))
                {
                    // FIXME: needs some checking to make sure the fleet has no ships
                    // FIXME: needs to make sure the entity is removed from the game
                    if(factionInfoDB.Fleets.Contains(fleet))
                    {
                        factionInfoDB.Fleets.Remove(fleet);
                    }
                    else
                    {
                        var fleetInfo = fleet.GetDataBlob<FleetDB>();
                        fleetInfo.ParentDB.Children.Remove(fleet);
                    }
                }
                ImGui.EndPopup();
            }
        }

        private void DisplayDropTarget(Entity fleet)
        {
            // Begin Drag Target
            if (ImGui.BeginDragDropTarget())
            {
                ImGui.AcceptDragDropPayload("FLEET", ImGuiDragDropFlags.None);
                if(ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragEntity != Entity.InvalidEntity)
                {
                    // Remove the dragEntity from the parent tree or the facction Fleets
                    var sourceFleetInfo = dragEntity.GetDataBlob<FleetDB>();

                    // Check if nested
                    if(sourceFleetInfo.Root != dragEntity)
                    {
                        sourceFleetInfo.ParentDB.Children.Remove(dragEntity);
                    }

                    if(factionInfoDB.Fleets.Contains(dragEntity))
                    {
                        factionInfoDB.Fleets.Remove(dragEntity);
                    }

                    // Drop the dragEntity
                    sourceFleetInfo.SetParent(fleet);
                    dragEntity = Entity.InvalidEntity;
                }
                ImGui.EndDragDropTarget();
            }
        }

        private void DisplayDropSource(Entity fleet, string name)
        {
            // Begin drag source
            if(ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceNoDisableHover))
            {
                dragEntity = fleet;

                ImGui.SetDragDropPayload("FLEET", IntPtr.Zero, 0);
                ImGui.Text(name);
                ImGui.EndDragDropSource();
            }
        }
    }
}