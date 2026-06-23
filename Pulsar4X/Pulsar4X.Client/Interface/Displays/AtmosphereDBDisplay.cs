using System;
using ImGuiNET;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Client
{
    public static class AtmosphereDBDisplay
    {
        /// <summary>Snapshot-based atmosphere display for UI ported to the API galaxy model.</summary>
        public static void Display(this Pulsar4X.Api.AtmosphereView atmosphere)
        {
            ImGui.PushID("atmosphere-display");
            if(ImGui.CollapsingHeader("Atmosphere", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);
                DisplayHelpers.PrintRow("Surface Temp", atmosphere.SurfaceTemperatureC.ToString("###,##0.00") + "°C");
                DisplayHelpers.PrintRow("Pressure", atmosphere.PressureAtm + " atm");
                if(atmosphere.Hydrosphere)
                {
                    DisplayHelpers.PrintRow("Hydrosphere", atmosphere.HydrosphereExtentPercent.ToString() + "%%");
                }

                foreach(var gas in atmosphere.Composition)
                {
                    var amountString = Math.Round(gas.Percent, 4) > 0 ? Stringify.Quantity(Math.Round(gas.Percent, 4)) + " %%" : "trace amounts";
                    DisplayHelpers.PrintRow(gas.Name, amountString);
                }
            }
            ImGui.PopID();
            ImGui.Columns(1);
        }

    }
}
