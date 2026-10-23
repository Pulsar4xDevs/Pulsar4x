using System;
using System.Linq;
using ImGuiNET;
using Pulsar4X.Client;
using Pulsar4X.Client.Interface;
using Pulsar4X.Colonies;
using Pulsar4X.Components;
using Pulsar4X.Engine;
using Pulsar4X.Factions;
using Pulsar4X.Industry;
using Pulsar4X.Industry.Orders;
using Pulsar4X.Storage;
using Pulsar4X.Weapons;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Client.Host;

/// <summary>Engine-object cargo display for the host's dev tooling (the player-facing UI uses the
/// snapshot-based overload in the UI library).</summary>
public static class CargoStorageDBDisplay
{
        public static void Display(this CargoStorageDB storage, Entity entity, GlobalUIState uiState, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
        {
            foreach(var (sid, storageType) in storage.TypeStores)
            {
                string header = entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoTypes[sid].Name + " Storage";
                string headerId = entity.GetFactionOwner.GetDataBlob<FactionInfoDB>().Data.CargoTypes[sid].UniqueID.ToString();
                double freeVolume = storage.GetFreeVolume(sid);
                double percent = ((storageType.MaxVolume - freeVolume) / storageType.MaxVolume) * 100;
                header += " (" + percent.ToString("0.#") + "% full)";

                ImGui.PushID(entity.Id.ToString());
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
            var entity = cargoStorageDB.OwningEntity;
            bool isColony = entity.HasDataBlob<ColonyInfoDB>();
            bool hasRate = entity.TryGetDataBlob<LocalConstructionDB>(out var construction)
                && construction.PointsPerDay > 0;
            bool isCrate = LocalConstructionJob.IsCrate(component.Design);
            var colonies = !isColony && hasRate && isCrate
                ? LocalConstructionProcessor.ColoniesInRange(entity)
                : new System.Collections.Generic.List<Entity>();
            bool showInstall = hasRate && isCrate;
            bool canQueue = showInstall && (isColony || colonies.Count > 0);

            ImGui.PushID(component.ID);
            if(ImGui.BeginPopupContextItem("###cargo-" + component.ID))
            {
                ImGui.Text(component.Name);
                ImGui.Separator();

                if(showInstall)
                {
                    if(!canQueue)
                        ImGui.BeginDisabled();
                    if(ImGui.MenuItem("Install") && canQueue)
                    {
                        if(isColony)
                        {
                            entity.Manager?.Game.OrderHandler.HandleOrder(
                                AddToConstructionQueueOrder.CreateMount(entity, component));
                        }
                        else
                        {
                            var ids = new System.Collections.Generic.List<int>(colonies.Count);
                            foreach(var colony in colonies)
                                ids.Add(colony.Id);
                            int picked = ColonyConstructionDisplay.PickedColony(entity.Id, ids);
                            Entity? target = null;
                            foreach(var colony in colonies)
                            {
                                if(colony.Id == picked)
                                    target = colony;
                            }
                            if(target != null)
                            {
                                entity.Manager?.Game.OrderHandler.HandleOrder(
                                    AddToConstructionQueueOrder.CreateMount(entity, component, target));
                            }
                        }
                    }
                    if(!canQueue)
                        ImGui.EndDisabled();
                    if(ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(canQueue
                            ? $"Queue a mount ({LocalConstructionJob.MountCost(component.MassPerUnit):N0} installation points)."
                            : "No colony in transfer range.");
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
