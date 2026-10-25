using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface;
using Pulsar4X.Client.Interface.Widgets;
// Engine usings: Stringify formatting, plus the deferred camera-pin / maneuver-panel bridges below.

namespace Pulsar4X.Client
{
    public record EntityCardEntity(int EntityId, string SystemId);

    internal static class EntityCardExtensions
    {
        private static int _idCounter = 0;

        /// <summary>
        /// Activates a card displaying the given entity.
        /// </summary>
        /// <param name="manager">The window manager instance.</param>
        /// <param name="entity">The entity requested for display.</param>
        public static EntityCard ActivateEntityCard(this WindowManager manager, EntityCardEntity entity)
        {
            void DeactivateAllOtherWindows(EntityCard keepActive)
            {
                if (manager.NamedWindowsByType.TryGetValue(typeof(EntityCard), out var windows))
                {
                    foreach (var window in windows.Cast<EntityCard>().Where(w => !ReferenceEquals(w, keepActive)))
                    {
                        window.SetActive(false);
                    }
                }
            }

            if (manager.NamedWindowsByType.TryGetValue(typeof(EntityCard), out var windowList))
            {
                // Reuse an existing inactive window if available
                foreach (var window in windowList.Cast<EntityCard>().Where(w => !w.GetActive()))
                {
                    window.SetEntity(entity.EntityId, entity.SystemId);
                    window.SetActive(true);
                    DeactivateAllOtherWindows(window);
                    return window;
                }
            }
            string name = "EntityCard|" + _idCounter++;
            var entityCard = new EntityCard(name, entity.EntityId, entity.SystemId);
            manager.AddNamedWindow(name, entityCard);
            entityCard.SetActive(true);
            DeactivateAllOtherWindows(entityCard);
            return entityCard;
        }
    }

    public class EntityCard : NamedPulsarGuiWindow
    {
        public int EntityId { get; private set; }
        public string SystemId { get; private set; }
        public string Title { get; private set; } = "Unknown";

        // Re-resolved each frame: system snapshots are replaced wholesale by server pushes.
        private EntitySnapshot? _entity;
        private IClientSystem? _system;
        private UserOrbitSettings.OrbitBodyType _bodyType;
        private CommanderSnapshot? _commander;

        private Vector4 _accentColor;
        private ColonyConstructionDisplay? _shipConstruction;

        // Animation constants
        private const float WindowWidth = 624f;
        private const float WindowHeight = 420f;
        private const float AnimationDuration = 0.2f; // seconds
        private const float BottomMargin = 4f;
        private const float RightMargin = 4f;

        // The Selector reads this so its list stops at the top of the open card.
        internal float ScreenTop { get; private set; }
        private float _laidOutHeight = 220f;

        // Animation state
        private enum AnimationState { Closed, Opening, Open, Closing }
        private AnimationState _animationState = AnimationState.Closed;
        private float _animationProgress = 0f;
        private DateTime _animationStartTime;

        internal EntityCard(string name, int entityId, string systemId) : base(name)
        {
            EntityId = entityId;
            SystemId = systemId;
            _flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar;
        }

        public void SetEntity(int entityId, string systemId)
        {
            EntityId = entityId;
            SystemId = systemId;
        }

        public new void SetActive(bool activeVal = true)
        {
            if (activeVal && !IsActive)
            {
                // Starting to open
                _animationState = AnimationState.Opening;
                _animationStartTime = DateTime.Now;
                _animationProgress = 0f;
                IsActive = true;
            }
            else if (!activeVal && IsActive)
            {
                // Starting to close
                _animationState = AnimationState.Closing;
                _animationStartTime = DateTime.Now;
                _animationProgress = 1f;
            }
        }

        public new void ToggleActive()
        {
            SetActive(!IsActive);
        }

        private float EaseOutCubic(float t)
        {
            return 1f - MathF.Pow(1f - t, 3f);
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }

        private Vector4 GetAccentColor()
        {
            return _bodyType switch
            {
                UserOrbitSettings.OrbitBodyType.Star => new Vector4(1.0f, 0.8f, 0.3f, 1.0f),
                UserOrbitSettings.OrbitBodyType.Planet => new Vector4(0.3f, 0.6f, 1.0f, 1.0f),
                UserOrbitSettings.OrbitBodyType.DwarfPlanet => new Vector4(0.65f, 0.5f, 0.8f, 1.0f),
                UserOrbitSettings.OrbitBodyType.Moon => new Vector4(0.7f, 0.75f, 0.85f, 1.0f),
                UserOrbitSettings.OrbitBodyType.Asteroid => new Vector4(0.75f, 0.55f, 0.3f, 1.0f),
                UserOrbitSettings.OrbitBodyType.Comet => new Vector4(0.4f, 0.85f, 0.95f, 1.0f),
                UserOrbitSettings.OrbitBodyType.Colony => new Vector4(0.3f, 0.85f, 0.4f, 1.0f),
                UserOrbitSettings.OrbitBodyType.Ship => new Vector4(0.4f, 0.6f, 0.9f, 1.0f),
                _ => new Vector4(0.5f, 0.5f, 0.55f, 1.0f),
            };
        }

        private void UpdateAnimation()
        {
            if (_animationState == AnimationState.Open || _animationState == AnimationState.Closed)
                return;

            float elapsed = (float)(DateTime.Now - _animationStartTime).TotalSeconds;
            float t = Math.Clamp(elapsed / AnimationDuration, 0f, 1f);

            if (_animationState == AnimationState.Opening)
            {
                _animationProgress = EaseOutCubic(t);
                if (t >= 1f)
                {
                    _animationState = AnimationState.Open;
                    _animationProgress = 1f;
                }
            }
            else if (_animationState == AnimationState.Closing)
            {
                _animationProgress = 1f - EaseInCubic(t);
                if (t >= 1f)
                {
                    _animationState = AnimationState.Closed;
                    _animationProgress = 0f;
                    IsActive = false;
                }
            }
        }

        internal override void Display()
        {
            if (!IsActive && _animationState == AnimationState.Closed) return;

            UpdateAnimation();

            // Don't render if fully closed. A window that just finished closing drops its
            // claim on the bottom edge. One that was already closed leaves the live card alone.
            if (_animationState == AnimationState.Closed)
            {
                if (!IsActive)
                    ScreenTop = 0;
                return;
            }

            _system = _uiState.GameClient?.Galaxy.GetSystem(SystemId);
            _entity = _system?.GetEntity(EntityId);

            // The entity left the faction's view (destroyed/hidden) — drop the window.
            if (_entity == null)
            {
                IsActive = false;
                _animationState = AnimationState.Closed;
                ScreenTop = 0;
                return;
            }

            _bodyType = UserOrbitSettings.FromBodyKind(_entity.Kind);
            _commander = FindCommander();
            Title = _entity.GetView<NameView>()?.Name ?? "Unknown";

            // The construction panes take the leftover height, so that one case stays a fixed window.
            // Everything else hugs its summary. Both sit on the bottom edge of the work area.
            bool shipConstruction = _commander == null
                && _bodyType == UserOrbitSettings.OrbitBodyType.Ship
                && _entity.GetView<ConstructionView>() is { IsShip: true };
            float width = shipConstruction ? WindowWidth : 480f;
            float height = shipConstruction ? WindowHeight : _laidOutHeight;
            var work = ImGui.GetMainViewport();
            float workRight = work.WorkPos.X + work.WorkSize.X;
            float workBottom = work.WorkPos.Y + work.WorkSize.Y;
            float finalX = workRight - width - RightMargin;
            float currentX = workRight + (finalX - workRight) * _animationProgress;
            float y = workBottom - height - BottomMargin;
            if (shipConstruction)
            {
                _flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar;
                ImGui.SetNextWindowSize(new Vector2(width, height), ImGuiCond.Always);
            }
            else
            {
                _flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize;
                ImGui.SetNextWindowSizeConstraints(new Vector2(width, 48f), new Vector2(width, work.WorkSize.Y - 8f));
            }
            ImGui.SetNextWindowPos(new Vector2(currentX, y), ImGuiCond.Always);
            ImGui.SetNextWindowBgAlpha(0.85f);

            var accentColor = GetAccentColor();

            // Remove window border
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);

            // Accent-colored collapsing headers
            ImGui.PushStyleColor(ImGuiCol.Header,
                new Vector4(accentColor.X * 0.15f, accentColor.Y * 0.15f, accentColor.Z * 0.15f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered,
                new Vector4(accentColor.X * 0.25f, accentColor.Y * 0.25f, accentColor.Z * 0.25f, 0.7f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive,
                new Vector4(accentColor.X * 0.2f, accentColor.Y * 0.2f, accentColor.Z * 0.2f, 0.8f));

            // Track if window is closed via the X button
            bool windowOpen = true;
            string kindLabel = _commander != null ? _commander.Kind.ToString() : _bodyType.ToDescription();
            if (Window.Begin(Title + " (" + kindLabel + ")" + "###" + EntityId, ref windowOpen, _flags))
            {
                _accentColor = accentColor;
                DrawWindowAccents(accentColor);
                DisplayHeader(accentColor);
                DisplayContent();
                float actual = ImGui.GetWindowSize().Y;
                float pinnedY = workBottom - actual - BottomMargin;
                ImGui.SetWindowPos(new Vector2(currentX, pinnedY));
                _laidOutHeight = actual;
                ScreenTop = pinnedY;
            }
            Window.End();

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();

            // Handle close button click
            if (!windowOpen && _animationState != AnimationState.Closing)
            {
                SetActive(false);
            }
        }

        private void DrawWindowAccents(Vector4 accentColor)
        {
            var drawList = ImGui.GetWindowDrawList();
            var winPos = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();

            // Top accent strip
            drawList.AddRectFilled(
                winPos,
                new Vector2(winPos.X + winSize.X, winPos.Y + 3f),
                ImGui.ColorConvertFloat4ToU32(accentColor));
        }

        private void DisplayHeader(Vector4 accentColor)
        {
            var drawList = ImGui.GetWindowDrawList();
            var winPos = ImGui.GetWindowPos();
            var winSize = ImGui.GetWindowSize();
            var contentStart = ImGui.GetCursorScreenPos();
            float startLocalY = ImGui.GetCursorPosY();
            float pinBtnSize = 16f;

            // Measure title line height with the header font
            ImGui.PushFont(Styles.MediumFont, 16f);
            float titleLineHeight = ImGui.GetTextLineHeight();
            ImGui.PopFont();

            // Entity subtitle (ship class, spectral type, body sub-type)
            string subtitle = GetEntitySubtitle();
            bool hasSubtitle = subtitle.Length > 0;

            // Second row always reserved for subtitle text + action buttons
            float textLineHeight = ImGui.GetTextLineHeight();
            float btnTotalHeight = pinBtnSize + ImGui.GetStyle().FramePadding.Y * 2;
            float secondRowHeight = Math.Max(textLineHeight, btnTotalHeight) + 4f;

            // Header dimensions
            float headerPad = 8f;
            float headerContentHeight = titleLineHeight + secondRowHeight;
            float headerTop = contentStart.Y - headerPad;
            float headerBottom = contentStart.Y + headerContentHeight + headerPad;

            // Header background (dark tinted with entity accent color)
            drawList.AddRectFilled(
                new Vector2(winPos.X, headerTop),
                new Vector2(winPos.X + winSize.X, headerBottom),
                ImGui.ColorConvertFloat4ToU32(
                    new Vector4(accentColor.X * 0.12f, accentColor.Y * 0.12f, accentColor.Z * 0.12f, 0.6f)));

            // Left accent bar
            drawList.AddRectFilled(
                new Vector2(winPos.X, headerTop),
                new Vector2(winPos.X + 3f, headerBottom),
                ImGui.ColorConvertFloat4ToU32(accentColor));

            // Bottom accent line
            drawList.AddLine(
                new Vector2(winPos.X, headerBottom),
                new Vector2(winPos.X + winSize.X, headerBottom),
                ImGui.ColorConvertFloat4ToU32(
                    new Vector4(accentColor.X, accentColor.Y, accentColor.Z, 0.4f)),
                1f);

            // Row 1: Title (left) + action buttons (right)
            float framePadX = ImGui.GetStyle().FramePadding.X * 2;
            float btnSpacing = 4f;
            float closeBtnWidth = pinBtnSize + framePadX;
            float pinBtnWidth = pinBtnSize + framePadX;
            float totalBtnsWidth = pinBtnWidth + btnSpacing + closeBtnWidth;
            float btnX = winSize.X - ImGui.GetStyle().WindowPadding.X - totalBtnsWidth;
            float btnY = startLocalY + (titleLineHeight - btnTotalHeight) * 0.5f;

            ImGui.PushFont(Styles.MediumFont, 16f);
            ImGui.Text(Title.ToUpper());
            ImGui.PopFont();

            // Pin button (right-aligned on row 1)
            ImGui.SetCursorPos(new Vector2(btnX, btnY));
            ImGui.PushID(EntityId);
            ImGui.PushStyleColor(ImGuiCol.Button, Styles.InvisibleColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                new Vector4(accentColor.X * 0.3f, accentColor.Y * 0.3f, accentColor.Z * 0.3f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,
                new Vector4(accentColor.X * 0.4f, accentColor.Y * 0.4f, accentColor.Z * 0.4f, 0.7f));
            if (ImGui.ImageButton("##headerpin", _uiState.Img_Pin().ToTextureRef(), new Vector2(pinBtnSize, pinBtnSize)))
            {
                _uiState.Camera.PinToEntity(EntityId, SystemId, _uiState);
            }
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(GlobalUIState.NamesForMenus[typeof(PinCameraBlankMenuHelper)]);

            // Close button
            ImGui.SameLine(0, btnSpacing);
            ImGui.PushStyleColor(ImGuiCol.Button, Styles.InvisibleColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                new Vector4(0.8f, 0.2f, 0.2f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,
                new Vector4(0.9f, 0.1f, 0.1f, 0.7f));
            if (ImGui.Button("X##headerclose", new Vector2(pinBtnSize + framePadX, pinBtnSize + ImGui.GetStyle().FramePadding.Y * 2)))
            {
                SetActive(false);
            }
            ImGui.PopStyleColor(3);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Close");
            ImGui.PopID();

            // Row 2: Subtitle (left) + type label (right)
            float secondRowY = startLocalY + titleLineHeight;
            ImGui.SetCursorPosY(secondRowY);

            if (hasSubtitle)
            {
                ImGui.PushStyleColor(ImGuiCol.Text,
                    new Vector4(accentColor.X * 0.8f, accentColor.Y * 0.8f, accentColor.Z * 0.8f, 0.6f));
                ImGui.Text(subtitle);
                ImGui.PopStyleColor();
            }


            // Ensure cursor is past the header background. SetCursorPos past the last
            // submitted item asserts unless a later item grows the window. A person, or
            // any other sparse entity, may submit nothing after this header.
            float headerLocalBottom = startLocalY + headerContentHeight + headerPad * 2;
            if (ImGui.GetCursorPosY() < headerLocalBottom)
                ImGui.SetCursorPosY(headerLocalBottom);
            ImGui.Dummy(Vector2.Zero);
        }

        private string GetEntitySubtitle()
        {
            if (_entity == null) return "";

            if (_commander != null)
            {
                string rank = _commander.RankName ?? "Rank " + _commander.Rank;
                return rank;
            }
            if (_entity.GetView<ShipView>() is { } ship)
                return ship.DesignName;
            if (_entity.GetView<StarView>() is { } star)
                return star.SpectralClass;
            if (_entity.GetView<BodyView>() is { } body)
            {
                if (GetParent() is { } parent)
                    return "Orbiting: " + (parent.GetView<NameView>()?.Name ?? "Unknown");
                return body.BodyType;
            }
            return "";
        }

        private EntitySnapshot? GetParent()
        {
            return _entity?.GetView<PositionView>()?.ParentId is { } parentId
                ? _system?.GetEntity(parentId)
                : null;
        }

        /// <summary>
        /// Resolves the colony associated with this body — either this entity itself, or the
        /// owned colony sitting on it.
        /// </summary>
        private EntitySnapshot? GetColony()
        {
            if (_entity == null || _system == null) return null;
            if (_entity.Kind == BodyKind.Colony) return _entity;

            return _system.Entities.FirstOrDefault(e => e.Kind == BodyKind.Colony
                && e.Relation == OwnerRelation.Owned
                && e.GetView<ColonyView>()?.PlanetEntityId == _entity.Id);
        }

        private CommanderSnapshot? FindCommander()
        {
            if (_entity == null)
                return null;
            var people = _uiState.GameClient?.Galaxy?.Commanders;
            if (people == null)
                return null;
            foreach (var person in people)
            {
                if (person.Id == _entity.Id)
                    return person;
            }
            return null;
        }

        private void DisplayCommander(CommanderSnapshot person)
        {
            var now = _uiState.GameClient?.Galaxy?.Time.GameDateTime ?? DateTime.UtcNow;

            ImGui.Columns(2, "##commander-info", false);
            ImGui.SetColumnWidth(0, 120);

            DisplayHelpers.PrintRow("Type", person.Kind.ToString());
            DisplayHelpers.PrintRow("Rank", person.RankName ?? "Rank " + person.Rank);
            DisplayHelpers.PrintRow(
                "Commissioned",
                person.CommissionedOn.ToShortDateString(),
                null,
                YearsBetween(person.CommissionedOn, now).ToString("0.0") + " years of service");
            DisplayHelpers.PrintRow(
                "Promoted",
                person.RankedOn.ToShortDateString(),
                null,
                YearsBetween(person.RankedOn, now).ToString("0.0") + " years in rank");
            DisplayHelpers.PrintRow("Experience", person.Experience + " / " + person.ExperienceCap);
            DisplayHelpers.PrintRow("Status", person.IsAssigned ? "Assigned" : "Available");
            if (person.AssignmentName != null)
                DisplayHelpers.PrintRow("Assignment", person.AssignmentName);

            ImGui.Columns(1);

            if (person.Bonuses.Count > 0)
            {
                ImGui.NewLine();
                DisplayHelpers.Header("Bonuses");
                foreach (var bonus in person.Bonuses)
                {
                    string valueStr = bonus.IsPercentage
                        ? (bonus.Value * 100).ToString("+0.#;-0.#") + "%"
                        : bonus.Value.ToString("+0.#;-0.#");
                    string bonusText = bonus.Name;
                    if (!string.IsNullOrEmpty(bonus.FilterName))
                        bonusText += " (" + bonus.FilterName + ")";

                    ImGui.PushStyleColor(ImGuiCol.Text, bonus.Value >= 0 ? Styles.GoodColor : Styles.BadColor);
                    ImGui.TextUnformatted(valueStr);
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.Text(bonusText);
                }
            }

            DisplayOrders();
        }

        private static double YearsBetween(DateTime from, DateTime to)
            => (to - from).TotalDays / 365.25;

        private void DisplayContent()
        {
            if (_entity == null) return;

            if (_commander != null)
            {
                DisplayCommander(_commander);
                return;
            }

            switch (_bodyType)
            {
                case UserOrbitSettings.OrbitBodyType.Ship:
                    DisplayShipContent();
                    break;
                case UserOrbitSettings.OrbitBodyType.Star:
                    DisplayStarContent();
                    break;
                case UserOrbitSettings.OrbitBodyType.Planet:
                case UserOrbitSettings.OrbitBodyType.DwarfPlanet:
                case UserOrbitSettings.OrbitBodyType.Moon:
                    DisplaySystemBodyContent();
                    break;
                case UserOrbitSettings.OrbitBodyType.Asteroid:
                case UserOrbitSettings.OrbitBodyType.Comet:
                    DisplaySmallBodyContent();
                    break;
                case UserOrbitSettings.OrbitBodyType.Colony:
                    DisplayColonyContent();
                    break;
                default:
                    DisplayGenericContent();
                    break;
            }
        }

        /// <summary>
        /// Owned colonies jump to Colony Management. A friendly colony's market stays here,
        /// because Colony Management only lists colonies this faction owns.
        /// </summary>
        private void DisplayColonyManagementLinks()
        {
            if (_entity == null || _system == null) return;

            if (_entity.Kind == BodyKind.Colony)
            {
                if (_entity.Relation != OwnerRelation.Owned) return;
                ImGui.Spacing();
                if (ImGui.Button("Colony Management", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                {
                    var window = ColonyManagementWindow.GetInstance();
                    window.SelectColony(_entity.Id, SystemId);
                    window.SetActive(true);
                }
                return;
            }

            foreach (var colony in _system.Entities)
            {
                if (colony.Relation != OwnerRelation.Owned) continue;
                if (colony.GetView<ColonyView>()?.PlanetEntityId != _entity.Id) continue;
                string name = colony.GetView<NameView>()?.Name ?? "Colony";
                ImGui.Spacing();
                if (ImGui.Button("Colony Management: " + name + "##colony-link-" + colony.Id,
                    new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                {
                    var window = ColonyManagementWindow.GetInstance();
                    window.SelectColony(colony.Id, SystemId);
                    window.SetActive(true);
                }
            }
        }

        private void DisplayFriendlyMarkets()
        {
            if (_entity == null || _system == null || _entity.Kind == BodyKind.Colony) return;

            foreach (var colony in _system.Entities)
            {
                if (colony.Relation == OwnerRelation.Owned) continue;
                if (colony.GetView<ColonyView>()?.PlanetEntityId != _entity.Id) continue;
                if (colony.GetView<MarketView>() is not { } market) continue;
                string name = colony.GetView<NameView>()?.Name ?? "Colony";
                if (ImGui.CollapsingHeader(name + "###market-" + colony.Id))
                {
                    MarketBarsDisplay.Display(colony, market, _uiState);
                    IntelDisplay.Display(colony, colony.GetView<IntelBookView>(), _system.SystemId, _uiState);
                }
            }
        }

        // --- Shared Helpers ---

        private void DisplayOrbitInfo()
        {
            var parent = GetParent();
            if (parent == null) return;

            ImGui.Columns(2, "##orbit-info", true);
            if (_entity!.GetView<WarpMovingView>() is { } warping)
            {
                DisplayHelpers.PrintRow("Warping", Stringify.Velocity(warping.SpeedMps));
            }
            else
            {
                DisplayHelpers.PrintFormattedCell("Orbiting");
                if (ImGui.SmallButton(parent.GetView<NameView>()?.Name ?? "Unknown"))
                {
                    _uiState.EntityClicked(parent.Id, _uiState.SelectedStarSystemId, MouseButtons.Primary);
                }
                ImGui.NextColumn();
                ImGui.Separator();
            }
            ImGui.Columns(1);
        }

        private void DisplayOrders()
        {
            var orders = _entity?.GetView<OrdersView>()?.Orders;
            if (orders == null || orders.Count == 0) return;

            if (ImGui.CollapsingHeader("Actions", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.BeginTable("OrdersTable", 3, Styles.TableFlags))
                {
                    ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthStretch, 0.1f);
                    ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthStretch, 0.3f);
                    ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch, 0.6f);
                    ImGui.TableHeadersRow();

                    for (int i = 0; i < orders.Count; i++)
                    {
                        ImGui.TableNextColumn();
                        ImGui.Text((i + 1).ToString());
                        ImGui.TableNextColumn();
                        ImGui.Text(orders[i].Name);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text("IsRunning: " + orders[i].IsRunning);
                            ImGui.Text("IsFinished: " + orders[i].IsFinished);
                            ImGui.EndTooltip();
                        }
                        ImGui.TableNextColumn();
                        ImGui.Text(orders[i].Details);
                    }

                    ImGui.EndTable();
                }
            }
        }

        private void DisplaySurveyInfo()
        {
            if (_entity?.GetView<GravSurveyView>() is { } gravSurvey && _system != null)
            {
                Displays.GravitationalAnomlay(gravSurvey, _entity, _system, _uiState);
            }
        }

        private void DisplayProgressIndicator()
        {
            if (_entity == null) return;

            var geoSurvey = _entity.GetView<GeoSurveyView>();
            bool isColonizeable = _entity.HasView<ColonizableView>();
            var colony = GetColony();

            // Once a colony on this body has infrastructure installed it's an established,
            // working colony — the survey/colonize progress is behind us, so show a live
            // infrastructure overview in place of the progress bar.
            var infrastructure = colony?.GetView<InfrastructureView>();
            if (colony != null && infrastructure is { HasInstalledInfrastructure: true })
            {
                DisplayInfrastructureOverview(colony, infrastructure);
                return;
            }

            if (geoSurvey == null && !isColonizeable && colony == null)
                return;

            var stages = new System.Collections.Generic.List<SurveyProgressBar.Stage>(3);

            stages.Add(new SurveyProgressBar.Stage(
                "Discovered",
                1f,
                "Discovered\nThis body has been detected and is visible on the system map."));

            if (geoSurvey != null)
            {
                const string rewardSummary =
                    "Reveals:\n"
                    + "  • Atmospheric composition\n"
                    + "  • Mineral deposits and their accessibility\n"
                    + "  • Surface conditions used to assess colonization";
                float fill = 0f;
                string tooltip;
                if (geoSurvey.HasSurveyStarted && geoSurvey.PointsRequired > 0)
                {
                    fill = (float)(geoSurvey.PercentComplete / 100.0);
                    if (geoSurvey.IsSurveyComplete)
                    {
                        tooltip = "Geological Survey\nComplete. Mineral and atmospheric data are available below.";
                    }
                    else
                    {
                        tooltip = "Geological Survey\nIn progress: " + geoSurvey.PercentComplete.ToString("0") + "%"
                            + " (" + geoSurvey.PointsCompleted + " / " + geoSurvey.PointsRequired + " survey points)\n\n"
                            + rewardSummary;
                    }
                }
                else
                {
                    tooltip = "Geological Survey\nNot started. Send a ship with geo-survey ability to scan this body.\n\n"
                        + rewardSummary;
                }
                stages.Add(new SurveyProgressBar.Stage("Geo Survey", fill, tooltip));
            }

            // A colony only counts as established once infrastructure is delivered, so the
            // final stage tracks infrastructure rather than mere presence of a colony.
            if (isColonizeable || colony != null)
            {
                string infraTooltip = colony != null
                    ? "Infrastructure\nThis body has a colony, but no infrastructure is installed yet. "
                      + "Deliver infrastructure here — build it elsewhere and ship it in, or construct it "
                      + "locally — to bring the colony online. Infrastructure provides the support capacity "
                      + "every other installation on the body draws on."
                    : "Infrastructure\nNo colony yet. Establish one by delivering infrastructure to this body: "
                      + "load it onto a freighter and unload it here. Infrastructure provides the support "
                      + "capacity every other installation draws on, so it must come first.";
                stages.Add(new SurveyProgressBar.Stage("Infrastructure", 0f, infraTooltip));
            }

            if (stages.Count == 0) return;

            SectionLabel("PROGRESS");
            ImGui.Indent();
            SurveyProgressBar.Draw("##entity-progress", stages, _accentColor);
            ImGui.Unindent();
        }

        private void DisplayInfrastructureOverview(EntitySnapshot colony, InfrastructureView infrastructure)
        {
            bool overCapacity = infrastructure.CapacityAvailable < 0;

            string colonyName = colony.GetView<NameView>()?.Name ?? "";
            SectionLabel(string.IsNullOrWhiteSpace(colonyName) ? "COLONY" : colonyName.ToUpperInvariant());

            // Single-line overview: capacity used vs provided, and the resulting output.
            // TextUnformatted: the literal '%' would be read as a printf specifier by ImGui.Text.
            string summary = $"{infrastructure.CapacityRequired:N0} / {infrastructure.CapacityProvided:N0} capacity"
                + $" · {infrastructure.Efficiency * 100:0}% output";

            const float cardPadding = 8f;
            float cardHeight = cardPadding * 2f + ImGui.GetTextLineHeightWithSpacing() * 2f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg,
                new Vector4(_accentColor.X, _accentColor.Y, _accentColor.Z, 0.06f));
            ImGui.PushStyleColor(ImGuiCol.Border,
                new Vector4(_accentColor.X, _accentColor.Y, _accentColor.Z, 0.35f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(cardPadding, cardPadding));

            if (ImGui.BeginChild("##infra-card", new Vector2(0f, cardHeight),
                ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted("Infrastructure");
                ImGui.PopStyleColor();

                ImGui.PushStyleColor(ImGuiCol.Text, overCapacity ? Styles.BadColor : Styles.DescriptiveColor);
                ImGui.TextUnformatted(summary);
                ImGui.PopStyleColor();
            }
            ImGui.EndChild();

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);
        }

        // --- Layout Helpers ---

        private void SectionLabel(string label)
        {
            ImGui.Spacing();
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            float availWidth = ImGui.GetContentRegionAvail().X;
            var labelSize = ImGui.CalcTextSize(label);
            float lineY = pos.Y + labelSize.Y * 0.5f;

            ImGui.PushStyleColor(ImGuiCol.Text,
                new Vector4(_accentColor.X * 0.7f, _accentColor.Y * 0.7f, _accentColor.Z * 0.7f, 0.8f));
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();

            drawList.AddLine(
                new Vector2(pos.X + labelSize.X + 8f, lineY),
                new Vector2(pos.X + availWidth, lineY),
                ImGui.ColorConvertFloat4ToU32(
                    new Vector4(_accentColor.X, _accentColor.Y, _accentColor.Z, 0.15f)));
        }

        private Vector4 GetHealthColor(float value)
        {
            if (value >= 0.75f) return Styles.GoodColor;
            if (value >= 0.50f) return Styles.OkColor;
            if (value >= 0.25f) return Styles.MediocreColor;
            return Styles.BadColor;
        }

        private void DrawRadialIndicator(
            ImDrawListPtr drawList, Vector2 center, float radius, float ringThickness,
            float value, string label, string centerText, bool isPlaceholder,
            string? extraTooltip = null)
        {
            var dimColor = new Vector4(
                _accentColor.X * 0.3f, _accentColor.Y * 0.3f, _accentColor.Z * 0.3f, 0.4f);
            var healthColor = isPlaceholder ? Styles.DescriptiveColor : GetHealthColor(value);
            var textColor = isPlaceholder ? Styles.DescriptiveColor : healthColor;

            drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(dimColor), 32, ringThickness);

            if (!isPlaceholder && value > 0f)
            {
                float startAngle = -MathF.PI / 2f;
                float endAngle = startAngle + value * 2f * MathF.PI;
                drawList.PathArcTo(center, radius, startAngle, endAngle, 32);
                drawList.PathStroke(ImGui.ColorConvertFloat4ToU32(healthColor), ImDrawFlags.None, ringThickness);
            }

            var centerTextSize = ImGui.CalcTextSize(centerText);
            drawList.AddText(
                new Vector2(center.X - centerTextSize.X * 0.5f, center.Y - centerTextSize.Y * 0.5f),
                ImGui.ColorConvertFloat4ToU32(textColor),
                centerText);

            var labelSize = ImGui.CalcTextSize(label);
            drawList.AddText(
                new Vector2(center.X - labelSize.X * 0.5f, center.Y + radius + ringThickness * 0.5f + 2f),
                ImGui.ColorConvertFloat4ToU32(Styles.DescriptiveColor),
                label);

            var min = new Vector2(center.X - radius - ringThickness, center.Y - radius - ringThickness);
            var max = new Vector2(center.X + radius + ringThickness, center.Y + radius + ringThickness);
            if (ImGui.IsMouseHoveringRect(min, max))
            {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted(label + ": " + (isPlaceholder ? "N/A" : (value * 100f).ToString("0") + "%"));
                if (extraTooltip != null)
                    ImGui.TextUnformatted(extraTooltip);
                ImGui.EndTooltip();
            }
        }

        private void DisplayShipStatusRow()
        {
            float radius = 22f;
            float ringThickness = 4f;
            float indicatorSpacing = 16f;
            float indicatorWidth = radius * 2f + indicatorSpacing;
            float availWidth = ImGui.GetContentRegionAvail().X;

            var cursorPos = ImGui.GetCursorScreenPos();
            float centerY = cursorPos.Y + radius + 4f;
            var drawList = ImGui.GetWindowDrawList();

            var ship = _entity!.GetView<ShipView>();
            var thrust = _entity.GetView<ThrustView>();

            float htkValue = 0f;
            string htkText = "-";
            float compValue = 0f;
            string compText = "-";
            float armorValue = 0f;
            string armorText = "N/A";
            bool armorPlaceholder = true;

            if (ship != null && ship.TotalComponents > 0)
            {
                htkValue = (float)ship.AverageComponentHealth;
                htkText = (htkValue * 100f).ToString("0") + "%";
                compValue = (float)ship.OperationalComponents / ship.TotalComponents;
                compText = ship.OperationalComponents + "/" + ship.TotalComponents;
            }

            if (ship != null && ship.ArmorThicknessMm > 0)
            {
                armorPlaceholder = false;
                armorValue = 1.0f;
                armorText = ship.ArmorThicknessMm.ToString("0.#") + "mm";
            }

            float dvValue = 0f;
            string dvText = "N/A";
            string? dvTooltip = null;
            bool dvPlaceholder = true;

            if (thrust != null && thrust.ExhaustVelocityMps > 0)
            {
                dvPlaceholder = false;
                double dv = thrust.DeltaVMps;
                dvTooltip = Stringify.Velocity(dv);

                if (dv >= 1e6)
                    dvText = (dv / 1e6).ToString("0.#") + "M";
                else if (dv >= 1e3)
                    dvText = (dv / 1e3).ToString("0.#") + "k";
                else
                    dvText = dv.ToString("0");

                if (dv > 0 && thrust.MaxDeltaVMps > 0)
                    dvValue = Math.Clamp((float)(dv / thrust.MaxDeltaVMps), 0f, 1f);
            }

            float span = indicatorWidth * 4f + radius * 2f;
            float x0 = cursorPos.X + Math.Max(0f, (availWidth - span) * 0.5f) + radius;

            DrawRadialIndicator(drawList, new Vector2(x0, centerY),
                radius, ringThickness, dvValue, "Δv", dvText, dvPlaceholder, dvTooltip);
            DrawRadialIndicator(drawList, new Vector2(x0 + indicatorWidth, centerY),
                radius, ringThickness, htkValue, "HTK", htkText, false);
            DrawRadialIndicator(drawList, new Vector2(x0 + indicatorWidth * 2f, centerY),
                radius, ringThickness, compValue, "COMP", compText, false);
            DrawRadialIndicator(drawList, new Vector2(x0 + indicatorWidth * 3f, centerY),
                radius, ringThickness, armorValue, "ARMOR", armorText, armorPlaceholder);
            DrawRadialIndicator(drawList, new Vector2(x0 + indicatorWidth * 4f, centerY),
                radius, ringThickness, 0f, "SHIELD", "N/A", true);

            ImGui.InvisibleButton("##statusRow", new Vector2(availWidth, radius * 2f + 24f));
        }

        // --- Type-Specific Content ---

        private void DisplayShipContent()
        {
            var ship = _entity!.GetView<ShipView>();

            DisplayShipStatusRow();

            string captain = string.IsNullOrEmpty(ship?.CommanderName) ? "No captain" : ship!.CommanderName!;
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextUnformatted("Captain");
            ImGui.PopStyleColor();
            ImGui.SameLine();
            ImGui.TextUnformatted(captain);

            if (_entity.GetView<OrdersView>() is { } ordersView)
            {
                string line = "Idle";
                string detail = "";
                string message = "";
                if (ordersView.goal.Name.Length > 0)
                {
                    line = ordersView.goal.Name;
                    detail = ordersView.goal.Status;
                    message = ordersView.goal.Message;
                }
                else if (ordersView.Orders.Count > 0)
                {
                    line = ordersView.Orders[0].Name;
                    if (ordersView.Orders.Count > 1)
                        detail = "+" + (ordersView.Orders.Count - 1);
                }

                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted("Now");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.TextUnformatted(line);
                bool hoverLine = ImGui.IsItemHovered();
                if (detail.Length > 0)
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextUnformatted(detail);
                    ImGui.PopStyleColor();
                }
                if (hoverLine && message.Length > 0)
                    ImGui.SetTooltip(message);
            }

            var parent = GetParent();
            if (parent != null)
            {
                if (_entity.GetView<WarpMovingView>() is { } warping)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextUnformatted("Warping");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.TextUnformatted(Stringify.Velocity(warping.SpeedMps));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextUnformatted("Orbiting");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    if (ImGui.SmallButton(parent.GetView<NameView>()?.Name ?? "Unknown"))
                        _uiState.EntityClicked(parent.Id, _uiState.SelectedStarSystemId, MouseButtons.Primary);
                }
            }

            if (_entity.GetView<ShipRefitView>() is { } refit)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted("Refit");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.TextUnformatted(refit.YardName + "  " + refit.PointsAccumulated + " / " + refit.PointCost);
            }

            if (_entity.Relation == OwnerRelation.Owned)
            {
                ImGui.Spacing();
                if (ImGui.Button("Fleet & Ship Management", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                    FleetWindow.GetInstance().OrderShip(EntityId);
            }

            // A ship with an installation rate keeps the construction section. Fleet Management
            // does not have it, and the cargo Install menu reads the colony picked here.
            if (_entity.GetView<ConstructionView>() is { IsShip: true } construction)
            {
                SectionLabel("CONSTRUCTION");
                _shipConstruction ??= new ColonyConstructionDisplay();
                float sectionHeight = Math.Max(280f, ImGui.GetContentRegionAvail().Y);
                if (ImGui.BeginChild("ShipConstruction", new Vector2(0, sectionHeight), ImGuiChildFlags.None))
                    _shipConstruction.Display(EntityId, construction, _uiState);
                ImGui.EndChild();

                if (_entity.GetView<CargoStorageView>() is { } storage)
                    storage.Display(EntityId, _uiState, ImGuiTreeNodeFlags.None);
            }
        }

        private void DisplayStarContent()
        {
            var star = _entity!.GetView<StarView>();
            var massVolume = _entity.GetView<MassVolumeView>();

            ImGui.Columns(2, "##star-info", true);

            if (star != null)
            {
                DisplayHelpers.PrintRow("Spectral Type", star.SpectralType + star.SpectralSubDivision);
                DisplayHelpers.PrintRow("Class", star.SpectralClass);
                DisplayHelpers.PrintRow("Temperature", star.SurfaceTemperatureC.ToString("#,##0") + " °C");
                DisplayHelpers.PrintRow("Luminosity", star.Luminosity + " " + star.LuminosityClass + " (" + star.LuminosityClassDescription + ")");
                DisplayHelpers.PrintRow("Age", Stringify.Quantity(star.AgeYears));
            }

            if (massVolume != null)
            {
                DisplayHelpers.PrintRow("Mass", Stringify.CelestialMass(massVolume.MassKg));
                DisplayHelpers.PrintRow("Radius", Stringify.Distance(massVolume.RadiusMetres));
                DisplayHelpers.PrintRow("Density", massVolume.DensityGramsPerCm3.ToString("##0.000") + " g/cm³");
            }

            if (star != null)
            {
                DisplayHelpers.PrintRow("Habitable Zone", star.MinHabitableRadiusAu.ToString("0.##") + " - " + star.MaxHabitableRadiusAu.ToString("0.##") + " AU");
            }

            ImGui.Columns(1);

            DisplayOrbitInfo();
            DisplaySurveyInfo();
        }

        private void DisplaySystemBodyContent()
        {
            bool isGeoSurveyed = _entity!.GetView<GeoSurveyView>()?.IsSurveyComplete ?? false;

            DisplayProgressIndicator();
            if (!isGeoSurveyed)
                IntelDisplay.OfferLine(_system, _entity.Id, IntelKind.Geo, "geo chart", _uiState);

            if (_entity.GetView<BodyView>() is { } body)
            {
                ImGui.TextUnformatted(body.GravityMetresPerSec2.ToString("0.##") + " m/s²  ·  "
                    + body.SurfaceTemperatureC.ToString("0.#") + " °C");
            }
            if (_entity.GetView<MassVolumeView>() is { } massVolume)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted(Stringify.CelestialMass(massVolume.MassKg)
                    + "  ·  " + Stringify.Distance(massVolume.RadiusMetres));
                ImGui.PopStyleColor();
            }

            bool ownedColony = GetColony()?.Relation == OwnerRelation.Owned;
            // Atmosphere and the rest of the body live on the colony summary once someone is there.
            if (isGeoSurveyed && !ownedColony && _entity.GetView<AtmosphereView>() is { } atmosphereView)
                atmosphereView.Display();

            DisplayColonyManagementLinks();
            DisplayFriendlyMarkets();

            if (isGeoSurveyed && _entity.GetView<MineralDepositsView>() is { } deposits
                && ImGui.CollapsingHeader("Minerals"))
            {
                deposits.Display(_entity.Id);
            }

            DisplaySurveyInfo();
        }

        private void DisplaySmallBodyContent()
        {
            bool isGeoSurveyed = _entity!.GetView<GeoSurveyView>()?.IsSurveyComplete ?? false;

            DisplayProgressIndicator();
            if (!isGeoSurveyed)
                IntelDisplay.OfferLine(_system, _entity.Id, IntelKind.Geo, "geo chart", _uiState);

            if (_entity.GetView<BodyView>() is { } body)
                ImGui.TextUnformatted(body.BodyType);

            if (_entity.GetView<MassVolumeView>() is { } massVolume)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted(Stringify.CelestialMass(massVolume.MassKg)
                    + "  ·  " + Stringify.Distance(massVolume.RadiusMetres));
                ImGui.PopStyleColor();
            }

            DisplayColonyManagementLinks();
            DisplayFriendlyMarkets();

            if (isGeoSurveyed && _entity.GetView<MineralDepositsView>() is { } deposits
                && ImGui.CollapsingHeader("Minerals"))
            {
                deposits.Display(_entity.Id);
            }

            DisplaySurveyInfo();
        }

        private void DisplayColonyContent()
        {
            if (_entity!.GetView<ColonyView>() is { } colonyView)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted("Population");
                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.TextUnformatted(Stringify.Quantity(colonyView.Population));
            }

            if (_entity.Relation == OwnerRelation.Owned)
            {
                if (_entity.GetView<InfrastructureView>() is { HasInstalledInfrastructure: true } infrastructure)
                {
                    bool overCapacity = infrastructure.CapacityAvailable < 0;
                    string summary = $"{infrastructure.CapacityRequired:N0} / {infrastructure.CapacityProvided:N0}"
                        + $"  ·  {infrastructure.Efficiency * 100:0}% output";
                    ImGui.PushStyleColor(ImGuiCol.Text, overCapacity ? Styles.BadColor : Styles.DescriptiveColor);
                    ImGui.TextUnformatted(summary);
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextUnformatted("No infrastructure");
                    ImGui.PopStyleColor();
                }
            }

            DisplayOrbitInfo();
            DisplayColonyManagementLinks();

            // Friendly and neutral colonies are not in Colony Management. The market stays here.
            if (_entity.Relation != OwnerRelation.Owned && _entity.GetView<MarketView>() is { } market)
            {
                MarketBarsDisplay.Display(_entity, market, _uiState);
                IntelDisplay.Display(_entity, _entity.GetView<IntelBookView>(), _system?.SystemId ?? "", _uiState);
            }
        }

        private void DisplayGenericContent()
        {
            if (_entity!.GetView<MassVolumeView>() is { } massVolume)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                ImGui.TextUnformatted(Stringify.Mass(massVolume.MassKg)
                    + "  ·  " + Stringify.Distance(massVolume.RadiusMetres));
                ImGui.PopStyleColor();
            }

            DisplayOrbitInfo();
            DisplayOrders();
            DisplayColonyManagementLinks();
            DisplayFriendlyMarkets();

            if (_entity.GetView<GeoSurveyView>()?.IsSurveyComplete == true
                && _entity.GetView<MineralDepositsView>() is { } deposits
                && ImGui.CollapsingHeader("Minerals"))
            {
                deposits.Display(_entity.Id);
            }

            DisplaySurveyInfo();
        }
    }
}
