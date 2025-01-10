using System.Linq;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Industry;
using Pulsar4X.Components;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine.Orders;
using System;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Weapons;

namespace Pulsar4X.SDL2UI
{
    public static class CargoStorageDBDisplay
    {
        public static void Display(this CargoStorageDB storage, EntityState entityState, GlobalUIState uiState, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
        {
            foreach(var (sid, storageType) in storage.TypeStores)
            {
                string header = entityState.Entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoTypes[sid].Name + " Storage";
                string headerId = entityState.Entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoTypes[sid].UniqueID.ToString();
                double freeVolume = storage.GetFreeVolume(sid);
                double percent = ((storageType.MaxVolume - freeVolume) / storageType.MaxVolume) * 100;
                header += " (" + percent.ToString("0.#") + "% full)";

                ImGui.PushID(entityState.Entity.Id.ToString());
                if(ImGui.CollapsingHeader(header + "###" + headerId, flags))
                {
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Total Volume", Stringify.VolumeLtr(storageType.MaxVolume));
                    DisplayHelpers.PrintRow("Available Volume", Stringify.VolumeLtr(freeVolume), null, null, false);
                    ImGui.Columns(1);

                    if(ImGui.BeginTable(header + "table", 3, Styles.TableFlags))
                    {
                        ImGui.TableSetupColumn("Item");
                        ImGui.TableSetupColumn("Quantity");
                        ImGui.TableSetupColumn("Volume");
                        ImGui.TableHeadersRow();

                        var cargoables = storageType.GetCargoables();
                        // Sort the display by the cargoables name
                        var sortedUnitsByCargoablesName = storageType.CurrentStoreInUnits.OrderBy(e => cargoables[e.Key].Name);

                        foreach(var (id, value) in sortedUnitsByCargoablesName)
                        {
                            ICargoable cargoType = cargoables[id];
                            var volumeStored = storage.GetVolumeStored(cargoType, true);

                            var itemCanStore = storage.GetFreeUnitSpace(cargoType);
                            var massStored = storage.GetMassStored(cargoType, true);
                            var massStoredLessEscro = storage.GetMassStored(cargoType, false);
                            var itemsStored = value;
                            var itemsStoredIncEscro = storage.GetUnitsStored(cargoType, true);
                            var itemsInEscro = CargoMath.GetUnitCountInEscro(storage, cargoType);

                            ImGui.TableNextColumn();
                            if(ImGui.Selectable(cargoType.Name, false, ImGuiSelectableFlags.SpanAllColumns)) {}
                            if(cargoType is Mineral)
                            {
                                var mineralSD = (Mineral)cargoType;
                                DisplayHelpers.DescriptiveTooltip(cargoType.Name, "Mineral", mineralSD.Description);
                            }
                            else if(cargoType is ProcessedMaterial)
                            {
                                var processedMaterialSD = (ProcessedMaterial)cargoType;
                                DisplayHelpers.DescriptiveTooltip(cargoType.Name, "Processed Material", processedMaterialSD.Description);
                            }
                            else if(cargoType is ComponentInstance)
                            {
                                var componentInstance = (ComponentInstance)cargoType;
                                DisplayHelpers.DescriptiveTooltip(cargoType.Name, componentInstance.Design.ComponentType, componentInstance.Design.Description);
                                AddContextMenu(storage, componentInstance, uiState);
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
                            ImGui.Text(Stringify.Quantity(itemsStored, "##.##"));
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("+" + Stringify.Quantity(itemsInEscro) + " in escro");
                                ImGui.Text("Mass: " + Stringify.Mass(massStored) + " (" + Stringify.Mass(cargoType.MassPerUnit) + " each)");
                                
                                ImGui.Text("can store " + Stringify.Quantity(itemCanStore) + " more items");
                                ImGui.EndTooltip();
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text(Stringify.VolumeLtr(volumeStored));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("Volume: " + Stringify.VolumeLtr(volumeStored) + " (" + Stringify.Volume(cargoType.VolumePerUnit, "#.#####") + " each)");
                                ImGui.EndTooltip();
                            }
                        }

                        ImGui.EndTable();
                    }
                }
                ImGui.PopID();
            }
        }

        private static void AddContextMenu(CargoStorageDB cargoStorageDB, ComponentInstance component, GlobalUIState uiState)
        {
            if(cargoStorageDB.OwningEntity == null) throw new InvalidOperationException($"OwningEntity for {cargoStorageDB} cannot be null");

            ImGui.PushID(component.Design.UniqueID.ToString());
            if(ImGui.BeginPopupContextItem("###" + component.Design.UniqueID))
            {
                ImGui.Text(component.Name);
                ImGui.Separator();

                bool canInstall = false;
                if(cargoStorageDB.OwningEntity.HasDataBlob<ColonyInfoDB>()
                    && component.Design.ComponentMountType.HasFlag(ComponentMountType.PlanetInstallation))
                    {
                        canInstall = true;
                    }
                else if(cargoStorageDB.OwningEntity.HasDataBlob<ShipInfoDB>()
                    && component.Design.ComponentMountType.HasFlag(ComponentMountType.ShipComponent))
                    {
                        canInstall = true;
                    }

                if(canInstall && !cargoStorageDB.TypeStores.ContainsKey(component.CargoTypeID))
                {
                    canInstall = false;
                }

                if(canInstall && ImGui.MenuItem("Install"))
                {
                    var storageOrder = RemoveComponentFromStorageOrder.Create(component.ParentEntity, component, 1);
                    uiState.Game.OrderHandler.HandleOrder(storageOrder);

                    var installOrder = InstallComponentInstanceOrder.Create(component.ParentEntity, component);
                    uiState.Game.OrderHandler.HandleOrder(installOrder);
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