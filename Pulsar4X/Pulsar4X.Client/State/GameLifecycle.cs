using System.Collections.Generic;
using Pulsar4X.Api;

namespace Pulsar4X.Client;

/// <summary>
/// The game-lifecycle seam: creating, loading and saving games is composition-root work (it
/// builds the engine `Game` and wires the in-process server/adapter), so the UI library drives it
/// through this engine-free interface and <c>Pulsar4X.Client.Host</c> implements it. The
/// implementation binds the player faction and builds the <see cref="IGameClient"/> before
/// returning; the UI finishes up from the returned <see cref="GameActivation"/>.
/// </summary>
public interface IGameLifecycle
{
    /// <summary>The installed mod manifests, for the new-game menu's mod-selection page.</summary>
    IReadOnlyList<ModOption> GetAvailableMods();

    /// <summary>Load the chosen mods and return the option catalog the new-game setup pages
    /// build their pick-lists from.</summary>
    NewGameCatalog LoadMods(IReadOnlyList<string> modManifestPaths);

    /// <summary>Create a game from the request and bring it up (faction bound, client built).
    /// Null when creation fails (e.g. no valid starting body).</summary>
    GameActivation? CreateNewGame(NewGameRequest request);

    /// <summary>Create a game instantly with default settings, bypassing the wizard.</summary>
    GameActivation? Quickstart();

    /// <summary>Load a saved game from disk and bring it up.</summary>
    GameActivation? LoadGame(string filePath);

    /// <summary>Save the running game to disk.</summary>
    void SaveGame(string filePath);
}

/// <summary>An installed mod, as listed on the new-game menu's mod-selection page.</summary>
public sealed record ModOption(
    string Name,
    string Version,
    string ManifestHash,
    string ManifestPath,
    bool EnabledByDefault);

public sealed record CatalogOption(string Id, string Name);

/// <summary>A mod-defined star system; <see cref="StartingBodies"/> are its bodies a new game can
/// start on (empty when the system can be included but not started in).</summary>
public sealed record CatalogSystemOption(string Id, string Name, IReadOnlyList<CatalogOption> StartingBodies);

/// <summary>The mod-defined options a new game is configured from (species pre-filtered to
/// playable ones).</summary>
public sealed record NewGameCatalog(
    IReadOnlyList<CatalogOption> Species,
    IReadOnlyList<CatalogOption> Themes,
    IReadOnlyList<CatalogOption> Colonies,
    IReadOnlyList<CatalogSystemOption> Systems);

/// <summary>Everything the composition root needs to create a game. <see cref="SystemId"/> may be
/// "random" to start in a randomly generated system.</summary>
public sealed record NewGameRequest(
    IReadOnlyList<string> ModManifestPaths,
    string FactionName,
    string FactionAbbreviation,
    string SpeciesId,
    string ColonyId,
    string SystemId,
    string BodyId,
    IReadOnlyList<string> EnabledSystems,
    int MaxSystems,
    int MasterSeed,
    int StartingFunds,
    bool EleStart,
    string SMPassword,
    string PlayerPassword);

/// <summary>
/// What the UI needs to finish bringing a game on screen. By the time this is returned the
/// lifecycle has already cleared old state, bound the player faction and built the game client;
/// the UI selects the system, points the camera and opens its default windows.
/// </summary>
public sealed record GameActivation(string SystemId)
{
    /// <summary>Where to centre the camera (the starting body), metres; null to leave it.</summary>
    public Vec3? CameraPositionM { get; init; }

    public float? CameraZoom { get; init; }
}
