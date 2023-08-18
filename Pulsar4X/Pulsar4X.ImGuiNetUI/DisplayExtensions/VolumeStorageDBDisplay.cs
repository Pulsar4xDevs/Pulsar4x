using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class VolumeStorageDBDisplay
    {
        public static void Display(this VolumeStorageDB storage, EntityState entityState, GlobalUIState uiState, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.DefaultOpen)
        {
            Vector2 size = ImGui.GetContentRegionAvail();

            foreach(var (sid, storageType) in storage.TypeStores)
            {
                string header = uiState.Game.StaticData.CargoTypes[sid].Name;
                string headerId = uiState.Game.StaticData.CargoTypes[sid].ID.ToString();
                double percent = ((storageType.MaxVolume - storageType.FreeVolume) / storageType.MaxVolume) * 100;
                header += " (" + percent.ToString("0.#") + "% full)";

                ImGui.PushID(entityState.Entity.Guid.ToString());
                if(ImGui.CollapsingHeader(header + "###" + headerId, flags))
                {
                    ImGui.Columns(4);
                    ImGui.Text("Item");
                    ImGui.NextColumn();
                    ImGui.Text("Quantity");
                    ImGui.NextColumn();
                    ImGui.Text("Mass");
                    ImGui.NextColumn();
                    ImGui.Text("Volume");
                    if(ImGui.IsItemHovered())
                        ImGui.SetTooltip("Max Volume: " + Stringify.Volume(storageType.MaxVolume) + "\nFree Volume: " + Stringify.Volume(storageType.FreeVolume));
                    ImGui.NextColumn();
                    ImGui.Separator();

                    foreach(var (id, value) in storageType.CurrentStoreInUnits)
                    {
                        ICargoable cargoType = storageType.Cargoables[id];
                        var volumeStored = value;
                        var volumePerItem = cargoType.VolumePerUnit;
                        var massStored = volumeStored * cargoType.MassPerUnit;
                        var itemsStored = storageType.CurrentStoreInUnits[id];

                        if(ImGui.Selectable(cargoType.Name)) {}
                        ImGui.NextColumn();
                        ImGui.Text(Stringify.Number(itemsStored));
                        ImGui.NextColumn();
                        ImGui.Text(Stringify.Mass(massStored));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip(Stringify.Mass(cargoType.MassPerUnit) + " per unit");
                        ImGui.NextColumn();
                        ImGui.Text(Stringify.Volume(volumeStored));
                        if(ImGui.IsItemHovered())
                            ImGui.SetTooltip(Stringify.Volume(volumePerItem) + " per unit");
                        ImGui.NextColumn();
                        //ImGui.SetTooltip(ctype.ToDescription);
                    }
                    ImGui.Columns(1);
                }
                ImGui.PopID();
            }
        }
    }
}