using System.Linq;
using ImGuiNET;
using System;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Client
{
    public static class CargoStorageDBDisplay
    {
        /// <summary>Snapshot-based cargo display for UI ported to the API galaxy model.
        /// <paramref name="holderId"/> is the colony/ship holding the cargo (the command target).</summary>
        public static void Display(this Pulsar4X.Api.CargoStorageView storage, int holderId, GlobalUIState uiState, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
        {
            foreach(var store in storage.Stores)
            {
                double percent = ((store.MaxVolume - store.FreeVolume) / store.MaxVolume) * 100;
                string header = store.TypeName + " Storage (" + percent.ToString("0.#") + "% full)";

                ImGui.PushID(holderId.ToString());
                if(ImGui.CollapsingHeader(header + "###" + store.TypeId, flags))
                {
                    ImGui.Columns(2);
                    DisplayHelpers.PrintRow("Total Volume", Stringify.VolumeLtr(store.MaxVolume));
                    DisplayHelpers.PrintRow("Available Volume", Stringify.VolumeLtr(store.FreeVolume), null, null, false);
                    ImGui.Columns(1);

                    if(ImGui.BeginTable(header + "table", 3, Styles.TableFlags))
                    {
                        ImGui.TableSetupColumn("Item");
                        ImGui.TableSetupColumn("Quantity");
                        ImGui.TableSetupColumn("Volume");
                        ImGui.TableHeadersRow();

                        foreach(var item in store.Items)
                        {
                            ImGui.TableNextColumn();
                            if(ImGui.Selectable(item.Name, false, ImGuiSelectableFlags.SpanAllColumns)) {}
                            if(item.ItemKind.Length > 0)
                            {
                                DisplayHelpers.DescriptiveTooltip(item.Name, item.ItemKind, item.Description);
                            }
                            if(item.CanInstall)
                            {
                                AddContextMenu(item, holderId, uiState);
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text(Stringify.Quantity(item.Units, "##.##"));
                            if(ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("+" + Stringify.Quantity(item.UnitsInEscrow) + " in escro");
                                ImGui.Text("Mass: " + Stringify.Mass(item.MassStoredKg) + " (" + Stringify.Mass(item.MassPerUnitKg) + " each)");

                                ImGui.Text("can store " + Stringify.Quantity(item.FreeUnitSpace) + " more items");
                                ImGui.EndTooltip();
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text(Stringify.VolumeLtr(item.VolumeStored));
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("Volume: " + Stringify.VolumeLtr(item.VolumeStored) + " (" + Stringify.Volume(item.VolumePerUnit, "#.#####") + " each)");
                                ImGui.EndTooltip();
                            }
                        }

                        ImGui.EndTable();
                    }
                }
                ImGui.PopID();
            }
        }

        private static void AddContextMenu(Pulsar4X.Api.CargoItemView item, int holderId, GlobalUIState uiState)
        {
            ImGui.PushID(item.Id);
            if(ImGui.BeginPopupContextItem("###cargo-item-" + item.Id))
            {
                ImGui.Text(item.Name);
                ImGui.Separator();

                if(ImGui.MenuItem("Install"))
                {
                    uiState.GameClient?.SubmitCommandAsync(
                        new Pulsar4X.Api.InstallComponentCommand(holderId, item.Id));
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
