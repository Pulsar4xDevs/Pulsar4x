using System.Numerics;
using ImGuiNET;

namespace Pulsar4X.Client.Interface.Themes;

public class FuturisticTheme : ITheme
{
    public void Apply()
    {
        var style = ImGui.GetStyle();
        var colors = style.Colors;

        // Main colors
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.10f, 0.95f);      // Darker blue-tinted background
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.12f, 0.94f);       // Slightly lighter than window
        colors[(int)ImGuiCol.Border] = new Vector4(0.24f, 0.46f, 0.98f, 0.25f);        // Subtle glowing blue border
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);  // No shadow

        // Text
        colors[(int)ImGuiCol.Text] = new Vector4(0.85f, 0.85f, 0.85f, 1.00f);          // Bright but not pure white
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);  // Gray

        // Headers and titlebars
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.04f, 0.04f, 0.08f, 1.00f);       // Dark background
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.16f, 0.29f, 0.48f, 1.00f); // Glowing blue when active
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.00f, 0.00f, 0.00f, 0.51f);

        // Buttons and interactive elements
        colors[(int)ImGuiCol.Button] = new Vector4(0.15f, 0.28f, 0.47f, 0.40f);        // Semi-transparent blue
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.24f, 0.46f, 0.98f, 0.63f);  // Brighter blue glow
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.39f, 0.61f, 1.00f, 0.95f);   // Brightest when clicked

        // Frame backgrounds (for checkbox, radio button, plot, slider, text input)
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.15f, 0.15f, 0.22f, 0.54f);       // Subtle contrast
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.24f, 0.46f, 0.98f, 0.27f); // Gentle glow
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.24f, 0.46f, 0.98f, 0.39f);  // Stronger glow

        // Tabs
        colors[(int)ImGuiCol.Tab] = new Vector4(0.11f, 0.20f, 0.33f, 0.86f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(0.24f, 0.46f, 0.98f, 0.80f);
        colors[(int)ImGuiCol.TabActive] = new Vector4(0.19f, 0.34f, 0.58f, 1.00f);
        colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.07f, 0.10f, 0.15f, 0.97f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.14f, 0.26f, 0.42f, 1.00f);

        // Sliders and scrollbars
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.24f, 0.46f, 0.98f, 0.78f);    // Bright blue
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.41f, 0.59f, 1.00f, 0.80f);
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.2f, 0.53f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.31f, 0.31f, 0.91f, 0.25f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.41f, 0.41f, 0.91f, 0.25f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.51f, 0.51f, 0.91f, 0.25f);

        // Table headers
        colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.31f, 0.31f, 0.35f, 1.00f);
        colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.23f, 0.23f, 0.25f, 1.00f);

        // Style
        style.WindowPadding = new Vector2(8, 8);
        style.FramePadding = new Vector2(5, 3);
        style.CellPadding = new Vector2(4, 2);
        style.ItemSpacing = new Vector2(6, 4);
        style.ItemInnerSpacing = new Vector2(4, 4);
        style.TouchExtraPadding = new Vector2(0, 0);

        style.IndentSpacing = 21.0f;
        style.ScrollbarSize = 10.0f;
        style.GrabMinSize = 9.0f;

        style.WindowBorderSize = 1.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupBorderSize = 1.0f;
        style.FrameBorderSize = 1.0f;
        style.TabBorderSize = 1.0f;

        style.WindowRounding = 0.0f;
        style.ChildRounding = 0.0f;
        style.FrameRounding = 0.0f;
        style.PopupRounding = 0.0f;
        style.ScrollbarRounding = 0.0f;
        style.GrabRounding = 0.0f;
        style.TabRounding = 0.0f;
    }
}