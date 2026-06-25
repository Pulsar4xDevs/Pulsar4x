using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;

namespace Pulsar4X.Client
{
    public class Selector : UniquePulsarGuiWindow<Selector>
    {
        // When true the window shows the section editor instead of its normal content.
        private bool _editing = false;

        // The sections of the selector, in display order, along with whether each is shown.
        private static readonly string[] _sectionNames =
        {
            "Corporation", "Systems", "Celestial Bodies", "Colonies", "Fleets"
        };
        private readonly Dictionary<string, bool> _sectionVisible = new ()
        {
            { "Corporation", true },
            { "Systems", true },
            { "Celestial Bodies", true },
            { "Colonies", true },
            { "Fleets", true },
        };

        // Indentation (in pixels) applied per level of a hierarchy (celestial bodies, fleets).
        private const float IndentStep = 12f;

        // The celestial body kinds listed in the "Celestial Bodies" section. Colonies and
        // ships are intentionally excluded as they have their own sections above.
        private static readonly BodyKind[] _celestialBodyKinds = new []
        {
            BodyKind.Star,
            BodyKind.Planet,
            BodyKind.DwarfPlanet,
            BodyKind.Moon,
            BodyKind.Asteroid,
            BodyKind.Comet,
        };

        //constructs the toolbar with the given buttons
        private Selector()
        {
            _flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground;
        }

        internal static Selector GetInstance()
        {
            if (!_uiState.LoadedWindows.ContainsKey(typeof(Selector)))
            {
                return new Selector();
            }

            return (Selector)_uiState.LoadedWindows[typeof(Selector)];
        }

        internal override void Display()
        {
            if(!IsActive || !_uiState.IsGameLoaded) return;

            ImGui.SetNextWindowSize(new Vector2(256, 0));
            ImGui.SetNextWindowPos(new Vector2(ImGui.GetMainViewport().WorkSize.X - 256, 0));
            ImGui.SetNextWindowBgAlpha(0);
            if(Window.Begin("###selector", _flags))
            {
                // TODO: re-implement this somewhere
                // SystemViewPreferences.GetInstance().DisplayCombo("map", selectedIndex =>
                // {
                //     _uiState.SelectedMapView = SystemViewPreferences.GetInstance().GetViewByIndex(selectedIndex);
                // });
                if(_editing)
                {
                    DisplayEditor();
                }
                else
                {
                    DisplaySections();
                }
            }
            Window.End();
        }

        private void DisplaySections()
        {
            // The gear button lives on the first visible section's header. If everything
            // is hidden we still need a way back into the editor, so draw a lone gear.
            string? firstVisible = Array.Find(_sectionNames, s => _sectionVisible[s]);
            if(firstVisible == null)
            {
                DrawGearButton(sameLine: false);
                return;
            }

            if(_sectionVisible["Corporation"])
                Section("Corporation", CorporationHeaderLabel(), firstVisible == "Corporation", DisplayCorporation);
            if(_sectionVisible["Systems"])
                Section("Systems", "Systems", firstVisible == "Systems", DisplaySystems);
            if(_sectionVisible["Celestial Bodies"])
                Section("Celestial Bodies", "Celestial Bodies", firstVisible == "Celestial Bodies", DisplayBodies);
            if(_sectionVisible["Colonies"])
                Section("Colonies", "Colonies", firstVisible == "Colonies", DisplayColonies);
            if(_sectionVisible["Fleets"])
                Section("Fleets", "Fleets", firstVisible == "Fleets", DisplayFleets);
        }

        /// <summary>
        /// Draws a collapsing header for a section, optionally with the settings gear
        /// button on the right of the header line, then the section content when open.
        /// </summary>
        private void Section(string sectionId, string headerLabel, bool drawGear, Action content)
        {
            if(drawGear) ImGui.SetNextItemAllowOverlap();
            bool open = ImGui.CollapsingHeader($"{headerLabel}###section-{sectionId}", ImGuiTreeNodeFlags.DefaultOpen);
            if(drawGear) DrawGearButton(sameLine: true);
            if(open) content();
        }

        private void DrawGearButton(bool sameLine)
        {
            var style = ImGui.GetStyle();
            string gear = "⚙"; // U+2699, merged in from DejaVuSans
            float btnWidth = ImGui.CalcTextSize(gear).X + style.FramePadding.X * 2f;

            if(sameLine) ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - btnWidth - style.WindowPadding.X);
            if(ImGui.SmallButton($"{gear}##selector-gear"))
            {
                _editing = true;
            }
            if(ImGui.IsItemHovered())
                ImGui.SetTooltip("Configure sections");
        }

        private void DisplayEditor()
        {
            ImGui.TextDisabled("Sections");
            ImGui.Separator();

            foreach(var name in _sectionNames)
            {
                bool visible = _sectionVisible[name];
                if(ImGui.Checkbox(name, ref visible))
                    _sectionVisible[name] = visible;
            }

            ImGui.Separator();

            // Save button, horizontally centered, exits editing mode.
            const float buttonWidth = 80f;
            float regionWidth = ImGui.GetContentRegionAvail().X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (regionWidth - buttonWidth) * 0.5f);
            if(ImGui.Button("Save", new Vector2(buttonWidth, 0)))
            {
                _editing = false;
            }
        }

        private static string CorporationHeaderLabel()
        {
            var faction = _uiState.GameClient?.Galaxy.Faction;
            if(faction == null) return "Corporation";
            return $"{faction.Name} [{faction.Abbreviation}]";
        }

        private static void DisplayCorporation()
        {
            var faction = _uiState.GameClient?.Galaxy.Faction;
            if(faction == null) return;

            string label = "Funds";
            string value = faction.Funds.ToString("C0", CultureInfo.CurrentCulture);

            // Get available width in current line
            float availWidth = ImGui.GetContentRegionAvail().X;

            // Calculate the width of the value text
            Vector2 valueSize = ImGui.CalcTextSize(value);

            // Calculate how many spaces we need to add
            float textWidth = ImGui.CalcTextSize(label).X + valueSize.X;
            float remainingWidth = availWidth - textWidth;


            // Create a padding string
            string padding = "";
            if (remainingWidth > 0)
            {
                // Estimate how many spaces we need based on space width
                float spaceWidth = ImGui.CalcTextSize(" ").X;
                int spacesNeeded = (int)(remainingWidth / spaceWidth);
                padding = new string(' ', Math.Max(0, spacesNeeded));
            }

            // Create the selectable with the label, padding, and value
            ImGui.Selectable($"{label}{padding}{value}");
        }

        private void DisplaySystems()
        {
            // Read the faction-scoped system summaries from the client galaxy model rather than
            // touching engine objects directly. KnownSystems stays current via the adapter's event stream.
            var galaxy = _uiState.GameClient?.Galaxy;
            if (galaxy == null) return;

            foreach (var system in galaxy.KnownSystems.OrderBy(s => s.Name))
            {
                if (ImGui.Selectable(system.Name, _uiState.SelectedStarSystemId.Equals(system.SystemId)))
                {
                    _uiState.SetActiveSystem(system.SystemId);
                }
            }
        }

        private static void DisplayBodies()
        {
            if (_uiState.GameClient == null) return;

            var system = _uiState.GameClient?.Galaxy.GetSystem(_uiState.SelectedStarSystemId);
            if (system == null) return;

            // Gather all celestial bodies in the system keyed by entity id so we can
            // reconstruct the orbital hierarchy (stars -> planets -> moons etc).
            var bodies = system.Entities
                .Where(e => Array.IndexOf(_celestialBodyKinds, e.Kind) >= 0)
                .ToDictionary(e => e.Id);

            // Build parent -> children lists. A body whose parent isn't another
            // celestial body in this set is treated as a root (e.g. the primary star).
            var children = new Dictionary<int, List<EntitySnapshot>>();
            var roots = new List<EntitySnapshot>();
            foreach (var body in bodies.Values)
            {
                int? parentId = ParentIdOf(body);
                if (parentId is { } pid && pid != body.Id && bodies.ContainsKey(pid))
                {
                    if (!children.TryGetValue(pid, out var list))
                    {
                        list = new List<EntitySnapshot>();
                        children[pid] = list;
                    }
                    list.Add(body);
                }
                else
                {
                    roots.Add(body);
                }
            }

            var prefs = SystemViewPreferences.GetInstance();
            foreach (var root in SortBodies(roots))
            {
                DisplayBodyNode(root, children, prefs, 0);
            }
        }

        private static int? ParentIdOf(EntitySnapshot body)
            => body.GetView<OrbitView>()?.ParentId ?? body.GetView<PositionView>()?.ParentId;

        private static IEnumerable<EntitySnapshot> SortBodies(List<EntitySnapshot> bodies)
        {
            // Within a level, order inner -> outer by orbital distance (semi-major axis),
            // falling back to name for bodies that share a distance or lack an orbit.
            return bodies.OrderBy(GetOrbitalDistance).ThenBy(NameOf);
        }

        private static double GetOrbitalDistance(EntitySnapshot body)
        {
            // Bodies without an orbit (e.g. a system's primary star) sort to the end of
            // their level; in practice such bodies are roots on their own anyway.
            return body.GetView<OrbitView>()?.SemiMajorAxisKm ?? double.MaxValue;
        }

        private static string NameOf(EntitySnapshot body) => body.GetView<NameView>()?.Name ?? "";

        private static void DisplayBodyNode(EntitySnapshot body, Dictionary<int, List<EntitySnapshot>> children, SystemViewPreferences prefs, int visibleDepth)
        {
            var orbitType = UserOrbitSettings.FromBodyKind(body.Kind);

            // Respect the same view filters used by the system map. A filtered-out body
            // is skipped but we still recurse so its children stay in the tree, sliding
            // up to fill the gap rather than indenting under a hidden parent.
            bool visible = prefs.ShouldDisplay("map", orbitType);
            int childDepth = visibleDepth;

            if (visible)
            {
                float indent = visibleDepth * IndentStep;
                if (indent > 0) ImGui.Indent(indent);

                string name = NameOf(body);
                bool selected = _uiState.LastClickedEntity?.Id == body.Id;
                var shortName = UserOrbitSettings.OrbitBodyTypeShortNames[(int)orbitType];
                if (ImGui.Selectable($"{shortName}  {name}", selected))
                {
                    _uiState.EntityClicked(body.Id, _uiState.SelectedStarSystemId, MouseButtons.Primary);
                    if (body.GetView<PositionView>() is { } pos)
                        _uiState.Camera.CenterOnPosition(pos.AbsolutePosition.X, pos.AbsolutePosition.Y, pos.AbsolutePosition.Z);
                }

                if (ImGui.IsItemHovered())
                {
                    var tip = UserOrbitSettings.OrbitBodyTypeTooltips[(int)orbitType];
                    ImGui.SetTooltip($"{name} ({tip})");
                }

                if (indent > 0) ImGui.Unindent(indent);
                childDepth = visibleDepth + 1;
            }

            if (children.TryGetValue(body.Id, out var childList))
            {
                foreach (var child in SortBodies(childList))
                {
                    DisplayBodyNode(child, children, prefs, childDepth);
                }
            }
        }

        private static void DisplayColonies()
        {
            var galaxy = _uiState.GameClient?.Galaxy;
            if (galaxy == null) return;

            // The faction's colonies are owned colony entities sitting in its known (loaded) systems.
            var colonies = new List<(IClientSystem System, EntitySnapshot Colony)>();
            foreach (var summary in galaxy.KnownSystems)
            {
                var system = galaxy.GetSystem(summary.SystemId);
                if (system == null) continue;
                foreach (var colony in system.Entities.Where(e => e.Kind == BodyKind.Colony && e.Relation == OwnerRelation.Owned))
                    colonies.Add((system, colony));
            }

            var window = ColonyManagementWindow.GetInstance();
            int? selectedId = window.GetActive() ? window.SelectedColonyId : null;

            foreach (var (system, colony) in colonies.OrderBy(c => NameOf(c.Colony)))
            {
                bool selected = selectedId == colony.Id;
                if (ImGui.Selectable($"{NameOf(colony)}###colony-{colony.Id}", selected))
                {
                    _uiState.SetActiveSystem(system.SystemId);
                    window.SelectColony(colony.Id, system.SystemId);
                    window.SetActive(true);
                }
            }
        }

        private static void DisplayFleets()
        {
            var galaxy = _uiState.GameClient?.Galaxy;
            if (galaxy == null) return;

            foreach (var fleet in galaxy.Fleets)
            {
                DisplayFleetNode(fleet, 0);
            }
        }

        private static void DisplayFleetNode(FleetSnapshot fleet, int depth)
        {
            float indent = depth * IndentStep;
            if (indent > 0) ImGui.Indent(indent);

            bool selected = FleetWindow.GetInstance().GetActive() && FleetWindow.GetInstance().SelectedFleetId == fleet.Id;
            if (ImGui.Selectable($"{fleet.Name}###fleet-{fleet.Id}", selected))
            {
                FleetWindow.GetInstance().SelectFleet(fleet.Id);
                FleetWindow.GetInstance().SetActive(true);
            }

            if (ImGui.IsItemHovered())
            {
                void Callback()
                {
                    if (fleet.Orders.Count > 0)
                    {
                        ImGui.Text("Orders:");
                        foreach (var order in fleet.Orders)
                            ImGui.Text(order.Name);
                    }
                    else
                    {
                        ImGui.Text("No orders");
                    }
                }

                DisplayHelpers.DescriptiveTooltip(fleet.Name, fleet.OrbitingName ?? "Unknown", "", Callback);
            }

            if (indent > 0) ImGui.Unindent(indent);

            // Sub-fleets first, then this fleet's ships, both one level deeper so the
            // hierarchy reads top-down like the fleet window.
            foreach (var sub in fleet.SubFleets)
                DisplayFleetNode(sub, depth + 1);

            // Flagship first, then alphabetical so the lead ship is easy to spot.
            foreach (var ship in fleet.Ships.OrderByDescending(s => s.Id == fleet.FlagshipId).ThenBy(s => s.Name))
            {
                DisplayShipNode(ship, depth + 1, ship.Id == fleet.FlagshipId);
            }
        }

        private static void DisplayShipNode(ShipSnapshot ship, int depth, bool isFlagship)
        {
            float indent = depth * IndentStep;
            if (indent > 0) ImGui.Indent(indent);

            // A small marker distinguishes the fleet's flagship from the rest.
            string label = isFlagship ? $"⚑ {ship.Name}" : ship.Name;

            // Grey out ships that aren't in the system the player is currently viewing.
            bool inViewedSystem = ship.SystemId == _uiState.SelectedStarSystemId;
            if (!inViewedSystem)
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);

            bool selected = _uiState.LastClickedEntity?.Id == ship.Id;
            if (ImGui.Selectable($"{label}###ship-{ship.Id}", selected))
            {
                ShipClicked(ship);
            }

            if (!inViewedSystem)
                ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(isFlagship ? $"{ship.Name} (Flagship)" : ship.Name);

            if (indent > 0) ImGui.Unindent(indent);
        }

        private static void ShipClicked(ShipSnapshot ship)
        {
            // Surface the ship like a map click would: focus its system, open the entity window,
            // and centre the camera (using the ship's position from the galaxy snapshot).
            if (string.IsNullOrEmpty(ship.SystemId)) return;

            if (_uiState.SelectedStarSystemId != ship.SystemId)
                _uiState.SetActiveSystem(ship.SystemId);

            _uiState.EntityClicked(ship.Id, ship.SystemId, MouseButtons.Primary);

            var snapshot = _uiState.GameClient?.Galaxy.GetSystem(ship.SystemId)?.GetEntity(ship.Id);
            if (snapshot?.GetView<PositionView>() is { } pos)
                _uiState.Camera.CenterOnPosition(pos.AbsolutePosition.X, pos.AbsolutePosition.Y, pos.AbsolutePosition.Z);
        }
    }
}