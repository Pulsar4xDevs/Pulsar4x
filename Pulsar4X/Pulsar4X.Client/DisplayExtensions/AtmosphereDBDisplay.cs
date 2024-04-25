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
            if(ImGui.CollapsingHeader("Atmosphere", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);
                DisplayHelpers.PrintRow("Surface Temp", atmosphereDB.SurfaceTemperature.ToString("###,##0.00") + "°C");
                DisplayHelpers.PrintRow("Pressure", atmosphereDB.Pressure + " atm");
                if(atmosphereDB.Hydrosphere)
                {
                    DisplayHelpers.PrintRow("Hydrosphere", atmosphereDB.HydrosphereExtent.ToString() + "%%");
                }

                foreach(var (gas, amount) in atmosphereDB.CompositionByPercent)
                {
                    var blueprint = uiState.Game.AtmosphericGases[gas];
                    var amountString = Math.Round(amount, 4) > 0 ? Stringify.Quantity(Math.Round(amount, 4)) + " %%" : "trace amounts";
                    DisplayHelpers.PrintRow(blueprint.Name, amountString);
                }
            }
            ImGui.Columns(1);
        }
    }
}