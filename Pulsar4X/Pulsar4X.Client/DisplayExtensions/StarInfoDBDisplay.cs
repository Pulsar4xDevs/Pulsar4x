using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
{
    public static class StarInfoDBDisplay
    {
        public static void Display(this StarInfoDB starInfo, EntityState entityState, GlobalUIState uiState)
        {
            ImGui.Columns(2);
            DisplayHelpers.PrintRow("Spectral Type", starInfo.SpectralType.ToDescription() + starInfo.SpectralSubDivision);
            DisplayHelpers.PrintRow("Luminosity", starInfo.Luminosity + " " + starInfo.LuminosityClass.ToString() + " (" + starInfo.LuminosityClass.ToDescription() + ")");
            DisplayHelpers.PrintRow("Class", starInfo.Class);
            DisplayHelpers.PrintRow("Age", Stringify.Quantity(starInfo.Age));
            DisplayHelpers.PrintRow("Habitable Zone", starInfo.MinHabitableRadius_AU.ToString("0.##") + "AU - " + starInfo.MaxHabitableRadius_AU.ToString("0.##") + "AU");
            ImGui.Columns(1);
        }
    }
}