using System;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    public static class AtmosphereDBDisplay
    {
        public static void Display(this AtmosphereDB atmosphereDB, EntityState entityState, GlobalUIState uiState)
        {
            ImGui.Text("Surface Temp: " + atmosphereDB.SurfaceTemperature.ToString("###,##0.00") + "°C");
            ImGui.Text("Pressure: " + atmosphereDB.Pressure + " atm");
            if(atmosphereDB.Hydrosphere)
            {
                ImGui.Text("Hydrosphere: " + atmosphereDB.HydrosphereExtent.ToString() + "%%");
            }

            if(ImGui.BeginTable("###GasTable" + entityState.Entity.Id, 2))
            {
                ImGui.TableSetupColumn("Type");
                ImGui.TableSetupColumn("Percent");
                ImGui.TableHeadersRow();

                foreach(var (gas, amount) in atmosphereDB.CompositionByPercent)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(gas.Name);
                    ImGui.TableNextColumn();
                    if(Math.Round(amount, 4) > 0)
                    {
                        ImGui.Text(Stringify.Quantity(Math.Round(amount, 4)) + " %%");
                    }
                    else
                    {
                        ImGui.Text("trace amounts");
                    }
                }

                ImGui.EndTable();
            }
        }
    }
}