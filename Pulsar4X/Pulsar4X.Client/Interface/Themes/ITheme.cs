using System.Drawing;

namespace Pulsar4X.Client.Interface.Themes;

// TODO: add all the colors
public interface ITheme
{
    // Text
    Color Text { get; }
    Color TextDisabled { get; }

    // Buttons and interactive elements
    Color Button { get; }
    Color ButtonHovered { get; }
    Color ButtonActive { get; }

    // Set as the current ImGui theme
    void Apply();
}
