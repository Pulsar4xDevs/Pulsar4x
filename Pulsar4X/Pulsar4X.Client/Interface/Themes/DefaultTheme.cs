using System.Drawing;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Themes;

public class DefaultTheme : ITheme
{
    // Text
    public Color Text {
        get => Color.FromArgb(255, 230, 230, 230);
    }
    public Color TextDisabled {
        get => Color.FromArgb(255, 128, 128, 128);
    }

    // Buttons and interactive elements - ImGui dark defaults
    public Color Button {
        get => Color.FromArgb(102, 66, 150, 250);
    }
    public Color ButtonHovered {
        get => Color.FromArgb(255, 66, 150, 250);
    }
    public Color ButtonActive {
        get => Color.FromArgb(255, 15, 135, 250);
    }

    public void Apply()
    {
        // Use ImGui's built-in dark color scheme as the baseline
        ImGui.StyleColorsDark();
    }
}
