namespace Pulsar4X.Client;

/// <summary>
/// Client-only gate for the Production-tab Missing Inputs section, its Queue buttons,
/// and the job pin toggle and highlight that feed it.
/// Flip <see cref="Enabled"/> to true to turn the section back on. The engine missing-inputs
/// API is left alone either way.
/// </summary>
public static class MissingInputsUi
{
    /// <summary>When false, the Production tab hides Missing Inputs, Queue buttons, pin toggle and highlight.</summary>
    public const bool Enabled = false;
}
