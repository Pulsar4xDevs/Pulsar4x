using System;

namespace Pulsar4X.Client;

/// <summary>Where the UI library surfaces a registered development tool.</summary>
public enum DevToolPlacement
{
    /// <summary>A checkbox in the settings window's debug list.</summary>
    SettingsList,
    /// <summary>An image button on the main toolbar.</summary>
    Toolbar,
    /// <summary>An image button on the SM toolbar, shown only while game-master mode is on.</summary>
    SMToolbar,
    /// <summary>A button on the main menu (closes the menu when clicked).</summary>
    MainMenu,
}

/// <summary>
/// A development tool registered by the composition root (<c>Pulsar4X.Client.Host</c>). The
/// engine-backed debug/SM windows live in the host executable, not the UI library; the library
/// renders toggles for whatever was registered without referencing the tools themselves.
/// </summary>
public sealed record DevToolRegistration(
    /// <summary>Stable id, also the hotkey hook (e.g. "debug-window").</summary>
    string Key,
    string Label,
    Action Toggle,
    Func<bool> IsActive,
    DevToolPlacement Placement)
{
    /// <summary>When set, the toggle is only offered while the predicate passes
    /// (e.g. orbit-debug needs a clicked orbiting entity).</summary>
    public Func<bool>? IsAvailable { get; init; }

    /// <summary>The toolbar button image (for <see cref="DevToolPlacement.Toolbar"/> /
    /// <see cref="DevToolPlacement.SMToolbar"/>); a generic icon is used when unset.</summary>
    public Func<IntPtr>? ToolbarIcon { get; init; }

    /// <summary>Sort position among toolbar buttons; the built-in buttons run 130–190, so
    /// smaller puts a tool in front of them.</summary>
    public int Order { get; init; } = 1000;
}
