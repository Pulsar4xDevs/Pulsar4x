# Pulsar4X.Client — UI Reference

ImGui.NET + SDL2 immediate-mode UI. The only runnable application in the solution. Lives in `Pulsar4X/Pulsar4X.Client/`.

---

## Entry Point and Boot Sequence

```
Program.cs
    → new PulsarMainWindow()       // inherits ImGuiSDL2CSWindow
        → new GlobalUIState(this)  // singleton UI state
        → GalacticMapRender(...)   // initialise map renderer
    → PulsarMainWindow.Run()       // SDL2 event loop
        → Layout() called every frame
            → renders all active windows via Display()
```

---

## Directory Map

| Directory | Contents |
|-----------|---------|
| `IMGUISDL/` | SDL2 event loop, OpenGL context, ImGui bindings (`ImGuiSDL2CSWindow`, `ImGuiSDL2CSHelper`) |
| `Interface/Windows/` | All top-level game windows (see Window Inventory below) |
| `Interface/Displays/` | Reusable display panels embedded inside windows |
| `Interface/HUD/` | Always-visible HUD elements: toolbar, time controls, selector |
| `Interface/Menus/` | Main menu, new/load/save game, settings |
| `Interface/Widgets/` | Small reusable ImGui widgets (FileDialog, TextModal, ImageButton, Window helper) |
| `Interface/Themes/` | Visual themes (`FuturisticTheme`, `ITheme` interface) |
| `Rendering/` | Camera, system map rendering, OpenGL renderer, icons |
| `Rendering/Icons/` | Per-entity-type icon renderers (orbit ellipses, ship icons, star icons, warp lines) |
| `Rendering/ManuverNodes/` | Maneuver node drawing (delta-V planning on system map) |
| `State/` | UI state objects (GlobalUIState, SystemState, EntityState, OrderRegistry) |
| `ModFileEditing/` | In-game blueprint editor (components, minerals, tech, etc.) |
| `Input/` | Hot-key handlers for the system map |
| `Demos/` | ImGui demo/test screens |
| `CrashReports/` | Discord crash reporter |

---

## Window System

### Base Classes

| Class | Purpose |
|-------|---------|
| `PulsarGuiWindow` | Base for all UI windows. Has `Display()`, `IsActive`, `_lookedAtEntity`, `_uiState`. Unique (one instance per type). |
| `NonUniquePulsarGuiWindow` | Allows multiple instances — one per entity. Keyed by `"WindowType|entityId"` in `LoadedNonUniqueWindows`. |

### Window Registration

Windows are registered in `GlobalUIState.NamesForMenus` (a static dict mapping Type → menu label). This is what populates the right-click context menu on entities. To add a new window to the context menu, add an entry here.

Windows are stored in:
- `GlobalUIState.LoadedWindows: Dictionary<Type, PulsarGuiWindow>` — unique windows.
- `GlobalUIState.LoadedNonUniqueWindows: Dictionary<string, NonUniquePulsarGuiWindow>` — per-entity windows.

A window is opened by calling `StartDisplay()` (sets `IsActive = true`). It is closed by setting `IsActive = false`.

### ImGui Pattern

Every window follows the same immediate-mode pattern:
```csharp
internal override void Display()
{
    if (!IsActive || _lookedAtEntity == null) return;

    if (ImGui.Begin("Window Title", ref IsActive, _flags))
    {
        // render content here
        ImGui.End();
    }
}
```
`Display()` is called every frame. There is no state retained between frames in ImGui beyond what you store in class fields.

---

## Window Inventory

| Window | File | Status | Notes |
|--------|------|--------|-------|
| `SystemWindow` | `SystemWindow.cs` | ✅ Functional | Star system selector and planet list |
| `FleetWindow` | `FleetWindow.cs` | ✅ Functional | Fleet listing, selection, basic orders |
| `ColonyManagementWindow` | `ColonyManagementWindow.cs` | ✅ Partial | Colony list, tabs for panels |
| `PlanetaryWindow` | `PlanetaryWindow.cs` | ⚠️ Partial | General info ✅ / Mineral deposits ✅ / **Installations ❌ EMPTY** |
| `ShipDesignWindow` | `ShipDesignWindow.cs` | ✅ Functional | Ship design and component assignment |
| `ComponentDesignWindow` | `ComponentDesignWindow.cs` | ✅ Functional | Component designer with NCalc formulas |
| `FireControlWindow` | `FireControlWindow.cs` | ✅ Functional | Weapon and fire control assignment |
| `OrdnanceDesignWindow` | `OrdnanceDesignWindow.cs` | ✅ Functional | Missile design |
| `ResearchWindow` | `ResearchWindow.cs` | ✅ Functional | Research queue and tech tree |
| `LogisticsWindow` | `LogisticsWindow.cs` | ✅ Partial | Automated cargo routes |
| `NavWindow` | `NavWindow.cs` | ✅ Functional | Navigation planning |
| `WarpOrderWindow` | `WarpOrderWindow.cs` | ✅ Functional | Warp order issuance |
| `NewtonOrderWindow` | `NewtonOrderWindow.cs` | ✅ Functional | Newtonian thrust orders |
| `EntityInfoWindow` | `EntityInfoWindow.cs` | ✅ Functional | Generic entity data display |
| `DamageViewerWindow` | `DamageViewerWindow.cs` | ⚠️ Partial | Ship damage visualization |
| `CommanderWindow` | `CommanderWindow.cs` | ✅ Basic | People/commander display |
| `GalaxyWindow` | `GalaxyWindow.cs` | ✅ Functional | Galaxy map and system list |
| `SMWindow` | `SMWindow.cs` | ✅ Functional | Space Master debug controls |
| `Debug/*` | `Debug/*.cs` | ✅ Dev tools | Data viewer, blueprint inspector, entity inspector, performance monitor |

---

## GlobalUIState

The central hub for all UI state. Key fields:

```csharp
Engine.Game Game                          // reference to the running game
Entity Faction                            // the player's currently selected faction entity
Entity PlayerFaction                      // same (legacy duplicate, keep consistent)
StarSystem SelectedSystem                 // active star system view
string SelectedStarSysGuid               // key into StarSystemStates
Dictionary<string, SystemState> StarSystemStates
GalacticMapRender GalacticMap
SafeList<UpdateWindowState> UpdateableWindows
```

`EntityClickedEventHandler` fires when the player clicks an entity on the system map. Connected windows listen to this to update their displayed entity.

---

## Rendering Architecture

```
GalacticMapRender               ← galaxy-level (zoomed out, systems as dots)
    └─ SystemMapRendering       ← system-level (planets, ships, orbits)
           ├─ Camera            ← pan/zoom state, world↔screen coordinate transforms
           ├─ OpenGLRenderer    ← draws lines/triangles via SDL2+OpenGL
           └─ Icons/
               ├─ OrbitEllipseIcon   ← Kepler orbit visualization
               ├─ ShipIcon           ← ship position dot
               ├─ WarpMovingIcon     ← warp transit line
               ├─ NewtonMoveIcon     ← burn arc
               └─ SysBodyIcon        ← planet/star dot
```

Icons implement `IRenderer` and are created per-entity. They read their entity's `PositionDB` each frame to update position. Icon instances are stored in `SystemState`.

`ManuverNodes/` — the delta-V planning interface drawn on the system map when the user is creating maneuver orders.

---

## Display Panels (Interface/Displays/)

Reusable panels embedded in windows (not standalone windows):

| Panel | File | Purpose |
|-------|------|---------|
| `ColonyInfoDBDisplay` | `ColonyInfoDBDisplay.cs` | Population table |
| `ColonyPanel` | `ColonyPanel.cs` | Full colony overview panel |
| `IndustryDisplay` | `IndustryDisplay.cs` | Production queue display |
| `IndustryPanel` | `IndustryPanel.cs` | Full industry panel |
| `CargoStorageDBDisplay` | `CargoStorageDBDisplay.cs` | Cargo hold contents |
| `ComponentInstancesDBDisplay` | `ComponentInstancesDBDisplay.cs` | Installed components list |
| `MineralsDBDisplay` | `MineralsDBDisplay.cs` | Mineral deposits table |
| `AtmosphereDBDisplay` | `AtmosphereDBDisplay.cs` | Atmospheric composition |
| `CargoTransfer/` | `CargoTransferWindow.cs` etc. | Drag-and-drop cargo transfer |

---

## Adding a New Window

1. Create `Interface/Windows/MyNewWindow.cs` inheriting `PulsarGuiWindow` (unique) or `NonUniquePulsarGuiWindow` (per-entity).
2. Implement `Display()` with ImGui calls.
3. Add a static `GetInstance()` factory following the `PlanetaryWindow.GetInstance()` pattern.
4. Register in `GlobalUIState.NamesForMenus` to add it to the context menu, OR call `GetInstance(entity, state).StartDisplay()` from a toolbar button.
5. Ensure `IsActive` is set to `false` on close — ImGui does this automatically via the `ref IsActive` on `ImGui.Begin()`.

---

## Critical Gaps to Fill

### PlanetaryWindow.RenderInstallations() — EMPTY

```csharp
private void RenderInstallations()
{
    if (_lookedAtEntity != null && _lookedAtEntity.Entity.HasDataBlob<InstallationsDB>())
    {
        InstallationsDB tempInstallations = _lookedAtEntity.Entity.GetDataBlob<InstallationsDB>();
        // BODY IS EMPTY — nothing is rendered
    }
}
```

`tempInstallations.Installations` (Dictionary<string, float>) and `.WorkingInstallations` (Dictionary<string, int>) are available. Render a table of `typeID → count / working count`. This is the first UI task in Phase 2.

### GroundCombatWindow — MISSING ENTIRELY

No window exists for ground combat. When `GroundCombatDB` (to be created) is present on a colony entity, a new `GroundCombatWindow` should be reachable from `PlanetaryWindow` tabs and from the system map context menu.

---

## Gotchas

1. **ImGui is immediate-mode.** There is no retained widget state between frames. All data to display must be read from game state on every `Display()` call. Avoid expensive computations inside `Display()` — cache them in fields and update only when the relevant game entity changes.

2. **`PlanetaryWindow.old.cs` is excluded from compilation.** The `.csproj` has `<Compile Remove="PlanetaryManagement\PlanetaryWindow.old.cs" />`. Do not reference it. It is a legacy file, possibly with stale API usage.

3. **`GalaxyMap.cs` in the Rendering directory is also excluded.** `<Compile Remove="MapRendering\GalaxyMap.cs" />`. Not the same as `GalaxyWindow.cs`.

4. **Window instances are keyed by string name.** `NonUniquePulsarGuiWindow` uses `"WindowType|entityId"` as the key in `LoadedNonUniqueWindows`. If you rename a window class, existing open window references in `GlobalUIState` become orphaned (harmless but leaks memory). Always use the static `GetInstance()` factory.

5. **The `Helpers.RenderImgUITextTable()` helper** renders a 2-column or N-column table from `List<string[]>` row data. Use it for consistent formatting across the info panels — it handles column alignment.
