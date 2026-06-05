using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Factions;
using Pulsar4X.Fleets;
using Pulsar4X.Movement;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;

namespace Pulsar4X.Client
{
    public class Selector : PulsarGuiWindow
    {
        private List<string> _knownSystems = new ();
        private List<StarSystem> _filteredAndSortedSystems = new ();

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

        // The celestial body types listed in the "Celestial Bodies" section. Colonies and
        // ships are intentionally excluded as they have their own sections above.
        private static readonly UserOrbitSettings.OrbitBodyType[] _celestialBodyTypes = new []
        {
            UserOrbitSettings.OrbitBodyType.Star,
            UserOrbitSettings.OrbitBodyType.Planet,
            UserOrbitSettings.OrbitBodyType.DwarfPlanet,
            UserOrbitSettings.OrbitBodyType.Moon,
            UserOrbitSettings.OrbitBodyType.Asteroid,
            UserOrbitSettings.OrbitBodyType.Comet,
        };

        //constructs the toolbar with the given buttons
        private Selector()
        {
            _flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground;

            _uiState.OnStarSystemAdded += SystemAdded;
            _uiState.OnFactionChanged += FactionChanged;
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
            if(!IsActive || _uiState.Faction == null) return;
            if(_knownSystems.Count == 0) RefreshSystems();

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
            if(_uiState.Faction == null) return "Corporation";
            return $"{_uiState.Faction.GetFactionName()} [{_uiState.Faction.GetFactionAbbreviation()}]";
        }

        private void RefreshSystems()
        {
            _knownSystems = _uiState.Faction?.GetDataBlob<FactionInfoDB>().KnownSystems ?? new ();
            FilterAndSortSystems();
        }

        private void FilterAndSortSystems()
        {
            _filteredAndSortedSystems = _uiState.Game?.Systems
                                            .Where(s => _knownSystems.Contains(s.ID))
                                            .OrderBy(s => s.NameDB.OwnersName)
                                            .ToList() ?? new ();
        }

        private void FactionChanged(GlobalUIState state)
        {
            RefreshSystems();
        }

        private void SystemAdded(GlobalUIState state, string systemId)
        {
            if(!_knownSystems.Contains(systemId))
                _knownSystems.Add(systemId);

            FilterAndSortSystems();
        }

        private static void DisplayCorporation()
        {
            if(_uiState.Faction == null) return;
            if(!_uiState.Faction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
                return;

            string label = "Funds";
            string value = factionInfoDB.Money.GetCurrentFunds().ToString("C0", CultureInfo.CurrentCulture);

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
            foreach (var system in _filteredAndSortedSystems)
            {
                if (ImGui.Selectable(system.NameDB.OwnersName, _uiState.SelectedStarSystemId.Equals(system.ID)))
                {
                    _uiState.SetActiveSystem(system.ID);
                }
            }
        }

        private static void DisplayBodies()
        {
            if (_uiState.Faction == null) return;
            if (string.IsNullOrEmpty(_uiState.SelectedStarSystemId)
                || !_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSystemId))
                return;

            var systemState = _uiState.StarSystemStates[_uiState.SelectedStarSystemId];

            // Gather all celestial bodies in the system keyed by entity id so we can
            // reconstruct the orbital hierarchy (stars -> planets -> moons etc).
            var bodies = systemState.EntityStatesWithNames.Values
                .Where(e => Array.IndexOf(_celestialBodyTypes, e.BodyType) >= 0)
                .ToDictionary(e => e.Id);

            // Build parent -> children lists. A body whose parent isn't another
            // celestial body in this set is treated as a root (e.g. the primary star).
            var children = new Dictionary<int, List<EntityState>>();
            var roots = new List<EntityState>();
            foreach (var body in bodies.Values)
            {
                var parent = body.GetParent();
                if (parent != null && parent.Id != body.Id && bodies.ContainsKey(parent.Id))
                {
                    if (!children.TryGetValue(parent.Id, out var list))
                    {
                        list = new List<EntityState>();
                        children[parent.Id] = list;
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

        private static IEnumerable<EntityState> SortBodies(List<EntityState> bodies)
        {
            // Within a level, order inner -> outer by orbital distance (semi-major axis),
            // falling back to name for bodies that share a distance or lack an orbit.
            return bodies.OrderBy(GetOrbitalDistance).ThenBy(e => e.Name);
        }

        private static double GetOrbitalDistance(EntityState body)
        {
            // Bodies without an orbit (e.g. a system's primary star) sort to the end of
            // their level; in practice such bodies are roots on their own anyway.
            if (body.Entity.TryGetDataBlob<OrbitDB>(out var orbit))
                return orbit.SemiMajorAxis;
            return double.MaxValue;
        }

        private static void DisplayBodyNode(EntityState body, Dictionary<int, List<EntityState>> children, SystemViewPreferences prefs, int visibleDepth)
        {
            // Respect the same view filters used by the system map. A filtered-out body
            // is skipped but we still recurse so its children stay in the tree, sliding
            // up to fill the gap rather than indenting under a hidden parent.
            bool visible = prefs.ShouldDisplay("map", body.BodyType);
            int childDepth = visibleDepth;

            if (visible)
            {
                float indent = visibleDepth * IndentStep;
                if (indent > 0) ImGui.Indent(indent);

                bool selected = _uiState.LastClickedEntity?.Id == body.Id;
                var shortName = UserOrbitSettings.OrbitBodyTypeShortNames[(int)body.BodyType];
                if (ImGui.Selectable($"{shortName}  {body.Name}", selected))
                {
                    _uiState.EntityClicked(body, MouseButtons.Primary);
                    _uiState.Camera.CenterOnEntity(body.Entity);
                }

                if (ImGui.IsItemHovered())
                {
                    var tip = UserOrbitSettings.OrbitBodyTypeTooltips[(int)body.BodyType];
                    ImGui.SetTooltip($"{body.Name} ({tip})");
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
            if(_uiState.Faction == null) return;

            var colonies = _uiState.Faction.GetDataBlob<FactionInfoDB>().Colonies;

            foreach (var colony in colonies)
            {
                bool visible = ColonyManagementWindow.GetInstance().GetActive() && ColonyManagementWindow.GetInstance().SelectedEntity?.Entity.Id == colony.Id;
                if (ImGui.Selectable(colony.GetName(_uiState.Faction.Id), visible))
                {
                    if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSystemId) && _uiState.StarSystemStates[_uiState.SelectedStarSystemId].EntityStatesColonies.ContainsKey(colony.Id))
                    {
                        ColonyManagementWindow.GetInstance().SelectEntity(_uiState.StarSystemStates[_uiState.SelectedStarSystemId].EntityStatesColonies[colony.Id]);
                        ColonyManagementWindow.GetInstance().SetActive(true);
                    }
                }
            }
        }

        private static void DisplayFleets()
        {
            if(_uiState.Faction == null) return;

            var fleets = _uiState.Faction.GetDataBlob<FleetDB>().RootDB?.Children ?? new SafeList<Entity>();

            foreach (var fleet in fleets)
            {
                // Check if the entity is actually a ship; ships only appear nested under a fleet.
                if (fleet.HasDataBlob<ShipInfoDB>())
                    continue;

                DisplayFleetNode(fleet, 0);
            }
        }

        private static void DisplayFleetNode(Entity fleet, int depth)
        {
            if (_uiState.Faction == null) return;

            float indent = depth * IndentStep;
            if (indent > 0) ImGui.Indent(indent);

            bool selected = FleetWindow.GetInstance().GetActive() && FleetWindow.GetInstance().SelectedFleet?.Id == fleet.Id;
            string display = fleet.GetName(_uiState.Faction.Id);
            if (ImGui.Selectable(display, selected))
            {
                FleetWindow.GetInstance().SelectFleet(fleet);
                FleetWindow.GetInstance().SetActive(true);
            }

            fleet.TryGetDataBlob<FleetDB>(out var fleetDB);

            if (ImGui.IsItemHovered())
            {
                void Callback()
                {
                    if (fleet.TryGetDataBlob<OrderableDB>(out var orderableDb)
                    && orderableDb.ActionList.Count > 0)
                    {
                        ImGui.Text("Orders:");
                        for (int i = 0; i < orderableDb.ActionList.Count; i++)
                        {
                            ImGui.Text(orderableDb.ActionList[i].Name);
                        }
                    }
                    else
                    {
                        ImGui.Text("No orders");
                    }
                }

                var flagshipID = fleetDB?.FlagShipID ?? -9999;
                if (fleet.Manager?.TryGetEntityById(flagshipID, out var flagship) ?? false)
                {
                    var positionDB = flagship.GetDataBlob<PositionDB>();
                    DisplayHelpers.DescriptiveTooltip(display, positionDB.Parent?.GetName(_uiState.Faction.Id) ?? "Unknown", "", Callback);
                }
            }

            if (indent > 0) ImGui.Unindent(indent);

            if (fleetDB == null) return;

            // Recurse into sub-fleets first, then list this fleet's ships, both indented
            // one level deeper so the hierarchy reads top-down like the fleet window.
            foreach (var child in fleetDB.GetChildren())
            {
                if (child.HasDataBlob<FleetDB>())
                    DisplayFleetNode(child, depth + 1);
            }

            var ships = fleetDB.GetChildren().Where(c => !c.HasDataBlob<FleetDB>());
            int flagshipID2 = fleetDB.FlagShipID;
            // Flagship first, then alphabetical so the lead ship is easy to spot.
            foreach (var ship in ships.OrderByDescending(s => s.Id == flagshipID2).ThenBy(s => s.GetName(_uiState.Faction.Id)))
            {
                DisplayShipNode(ship, depth + 1, ship.Id == flagshipID2);
            }
        }

        private static void DisplayShipNode(Entity ship, int depth, bool isFlagship)
        {
            if (_uiState.Faction == null) return;

            float indent = depth * IndentStep;
            if (indent > 0) ImGui.Indent(indent);

            string name = ship.GetName(_uiState.Faction.Id);
            // A small marker distinguishes the fleet's flagship from the rest.
            string label = isFlagship ? $"⚑ {name}" : name;

            // Grey out ships that aren't in the system the player is currently viewing.
            bool inViewedSystem = ship.Manager?.ManagerID == _uiState.SelectedStarSystemId;
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
                ImGui.SetTooltip(isFlagship ? $"{name} (Flagship)" : name);

            if (indent > 0) ImGui.Unindent(indent);
        }

        private static void ShipClicked(Entity ship)
        {
            // Ships live in their star system's manager; surface them the same way a
            // map click would: focus the owning system, open the entity window, and
            // center the camera. Bail quietly if the ship isn't in a known system.
            var systemId = ship.Manager?.ManagerID;
            if (systemId == null
                || !_uiState.StarSystemStates.TryGetValue(systemId, out var systemState)
                || !systemState.EntityStatesWithNames.TryGetValue(ship.Id, out var shipState))
                return;

            if (_uiState.SelectedStarSystemId != systemId)
                _uiState.SetActiveSystem(systemId);

            _uiState.EntityClicked(shipState, MouseButtons.Primary);
            _uiState.Camera.CenterOnEntity(ship);
        }
    }
}