using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class VolumeStorageDBDisplay
    {
        public static void Display(this VolumeStorageDB storage, EntityState entityState, GlobalUIState uiState)
        {
            Vector2 size = ImGui.GetContentRegionAvail();
            ImGui.BeginChild("###Cargo" + entityState.Entity.Guid, size, true, ImGuiWindowFlags.AlwaysAutoResize);

            foreach(var (sid, storageType) in storage.TypeStores)
            {
                string header = uiState.Game.StaticData.CargoTypes[sid].Name;
                string headerId = uiState.Game.StaticData.CargoTypes[sid].ID.ToString();
                double percent = ((storageType.MaxVolume - storageType.FreeVolume) / storageType.MaxVolume) * 100;

                ImGui.PushID(entityState.Entity.Guid.ToString());
                if(ImGui.CollapsingHeader(header + " (" + percent.ToString("0.#") + "% full)###" + headerId, ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.Columns(4);
                    ImGui.Text("Item");
                    ImGui.NextColumn();
                    ImGui.Text("Quantity");
                    ImGui.NextColumn();
                    ImGui.Text("Mass");
                    ImGui.NextColumn();
                    ImGui.Text("Volume");
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
                        ImGui.NextColumn();
                        ImGui.Text(Stringify.Volume(volumeStored));
                        ImGui.NextColumn();
                        //ImGui.SetTooltip(ctype.ToDescription);
                    }
                    ImGui.Columns(1);
                }
                ImGui.PopID();
            }

            ImGui.EndChild();
        }
    }
}