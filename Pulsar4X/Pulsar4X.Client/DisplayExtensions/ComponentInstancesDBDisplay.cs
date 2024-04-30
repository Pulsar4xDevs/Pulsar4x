using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Components;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.SDL2UI
{
    public static class ComponentInstancesDBDisplay
    {
        public static void Display(this ComponentInstancesDB db, EntityState entityState, GlobalUIState uiState)
        {
            if(ImGui.BeginTable("InstallationTable", 3, Styles.TableFlags))
            {
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.None, 0.25f);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.None, 1f);
                ImGui.TableHeadersRow();

                // FIXME: we should probably not do this every frame
                var sortedData = db.ComponentsByDesign.Where(entry => entry.Value.Count > 0).OrderBy(entry => entry.Value.First().Name).ToDictionary(entry => entry.Key, entry => entry.Value);
                foreach(var (designID, listPerDesign) in sortedData)
                {
                    if(listPerDesign.Count == 0) continue;

                    var instance = listPerDesign[0];

                    ImGui.TableNextColumn();
                    ImGui.Text(instance.Name);
                    AddContextMenu(instance, uiState);
                    DisplayHelpers.DescriptiveTooltip(instance.Name, instance.Design.TypeName, instance.Design.Description, null, true);
                    ImGui.TableNextColumn();
                    ImGui.Text(listPerDesign.Count.ToString());
                    ImGui.TableNextColumn();

                    var onCount = listPerDesign.Where(x => x.IsEnabled).Count();
                    var offCount = listPerDesign.Where(x => !x.IsEnabled).Count();

                    if(onCount > 0 && offCount > 0)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                        ImGui.Text("Degraded");
                        ImGui.PopStyleColor();
                    }
                    else if(onCount == 0)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.BadColor);
                        ImGui.Text("Disabled");
                        ImGui.PopStyleColor();
                    }
                    else
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                        ImGui.Text("Operational");
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndTable();
            }
        }

        private static void AddContextMenu(ComponentInstance component, GlobalUIState uiState)
        {
            ImGui.PushID(component.Design.UniqueID.ToString());
            if(ImGui.BeginPopupContextItem("###" + component.Design.UniqueID))
            {
                ImGui.Text(component.Name);
                ImGui.Separator();
                if(component.Design.ComponentMountType.HasFlag(ComponentMountType.ShipCargo) && ImGui.MenuItem("Move to Storage"))
                {
                    // Check if the components parent has storage
                    if(component.ParentEntity.TryGetDatablob<VolumeStorageDB>(out var volumeStorageDB)
                        && volumeStorageDB.TypeStores.ContainsKey(component.CargoTypeID))
                    {
                        var uninstallOrder = UninstallComponentInstanceOrder.Create(component.ParentEntity, component, 1);
                        uiState.Game.OrderHandler.HandleOrder(uninstallOrder);

                        var storageOrder = AddComponentToStorageOrder.Create(component.ParentEntity, component, 1);
                        uiState.Game.OrderHandler.HandleOrder(storageOrder);
                    }
                }
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.TerribleColor);
                if(ImGui.MenuItem("Destroy"))
                {

                }
                ImGui.PopStyleColor();
                ImGui.EndPopup();
            }
            ImGui.PopID();
        }
    }
}