using ImGuiNET;
using Pulsar4X.ECSLib;

namespace Pulsar4X.SDL2UI
{
    public static class StarInfoDBDisplay
    {
        public static void Display(this StarInfoDB starInfo, EntityState entityState, GlobalUIState uiState)
        {
            ImGui.Text("Spectral Type: " + starInfo.SpectralType.ToDescription() + starInfo.SpectralSubDivision);
            ImGui.Text("Luminosity: " + starInfo.Luminosity + " " + starInfo.LuminosityClass.ToString() + " (" + starInfo.LuminosityClass.ToDescription() + ")");
            ImGui.Text("Class: " + starInfo.Class);
            ImGui.Text("Age: " + Stringify.Quantity(starInfo.Age));
            ImGui.Text("Habitable Zone: " + starInfo.MinHabitableRadius_AU.ToString("0.##") + "AU - " + starInfo.MaxHabitableRadius_AU.ToString("0.##") + "AU");
        }
    }
}