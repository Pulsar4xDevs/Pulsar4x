using System.Linq;
using ImGuiNET;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.ComponentFeatureSets.Missiles;

namespace Pulsar4X.SDL2UI
{
    public static class VolumeStorageDBDisplay
    {
        public static void Display(this VolumeStorageDB storage, EntityState entityState, GlobalUIState uiState, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
        {
            foreach(var (sid, storageType) in storage.TypeStores)
            {
                string header = uiState.Game.StaticData.CargoTypes[sid].Name + " Storage";
                string headerId = uiState.Game.StaticData.CargoTypes[sid].ID.ToString();
                double freeVolume = storage.GetFreeVolume(sid);
                double percent = ((storageType.MaxVolume - freeVolume) / storageType.MaxVolume) * 100;
                header += " (" + percent.ToString("0.#") + "% full)";

                ImGui.PushID(entityState.Entity.Guid.ToString());
                if(ImGui.CollapsingHeader(header + "###" + headerId, flags))
                {
                    if(ImGui.BeginTable(header + "table", 2, Styles.TableFlags))
                    {
                        ImGui.TableSetupColumn("Item");
                        ImGui.TableSetupColumn("Quantity");
                        ImGui.TableHeadersRow();

                        var cargoables = storageType.GetCargoables();
                        // Sort the display by the cargoables name
                        var sortedUnitsByCargoablesName = storageType.CurrentStoreInUnits.OrderBy(e => cargoables[e.Key].Name);

                        foreach(var (id, value) in sortedUnitsByCargoablesName)
                        {
                            ICargoable cargoType = cargoables[id];
                            var volumeStored = storage.GetVolumeStored(cargoType);
                            var massStored = storage.GetMassStored(cargoType);
                            var itemsStored = value;

                            ImGui.TableNextColumn();
                            if(ImGui.Selectable(cargoType.Name, false, ImGuiSelectableFlags.SpanAllColumns)) {}
                            if(cargoType is MineralSD)
                            {
                                var mineralSD = (MineralSD)cargoType;
                                DisplayHelpers.DescriptiveTooltip(cargoType.Name, "", mineralSD.Description);
                            }
                            else if(cargoType is ProcessedMaterialSD)
                            {
                                var processedMaterialSD = (ProcessedMaterialSD)cargoType;
                                DisplayHelpers.DescriptiveTooltip(cargoType.Name, "", processedMaterialSD.Description);
                            }
                            else if(cargoType is ComponentInstance)
                            {
                                var componentInstance = (ComponentInstance)cargoType;
                                DisplayHelpers.DescriptiveTooltip(cargoType.Name, componentInstance.Design.ComponentType, componentInstance.Design.Description);
                            }
                            else if(cargoType is ComponentDesign)
                            {
                                var componentDesign = (ComponentDesign)cargoType;
                                DisplayHelpers.DescriptiveTooltip(componentDesign.Name, componentDesign.ComponentType, componentDesign.Description);
                            }
                            else if(cargoType is OrdnanceDesign)
                            {
                                var ordnanceDesign = (OrdnanceDesign)cargoType;
                                var components = ordnanceDesign.Components.Select(tuple => tuple.design).ToArray();
                                foreach(var component in components)
                                {
                                    DisplayHelpers.DescriptiveTooltip(component.Name, component.ComponentType, component.Description);
                                }
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text(Stringify.Number(itemsStored, "#,###,###,###,##0"));
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("Mass: " + Stringify.Mass(massStored) + " (" + Stringify.Mass(cargoType.MassPerUnit) + " each)");
                                ImGui.Text("Volume: " + Stringify.Volume(volumeStored) + " (" + Stringify.Volume(cargoType.VolumePerUnit, "#.#####") + " each)");
                                ImGui.EndTooltip();
                            }
                        }

                        ImGui.EndTable();
                    }
                }
                ImGui.PopID();
            }
        }
    }
}