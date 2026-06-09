using System;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Client.Interface.Widgets;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Colonies;
using Pulsar4X.Factions;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;
using Pulsar4X.Client.Interface;
using Pulsar4X.Damage;
using Pulsar4X.People;

namespace Pulsar4X.Client
{
    public class EntityWindow : NonUniquePulsarGuiWindow
    {
        public Entity Entity { get; private set; }
        public EntityState EntityState { get; private set; }
        public string Title { get; private set; }

        private Vector4 _accentColor;

        // Animation constants
        private const float WindowWidth = 624f;
        private const float WindowHeight = 420f;
        private const float AnimationDuration = 0.2f; // seconds
        private const float BottomMargin = 4f;
        private const float RightMargin = 4f;

        // Animation state
        private enum AnimationState { Closed, Opening, Open, Closing }
        private AnimationState _animationState = AnimationState.Closed;
        private float _animationProgress = 0f;
        private DateTime _animationStartTime;

        public EntityWindow(EntityState entityState)
        {
            Entity = entityState.Entity;
            EntityState = entityState;
            _flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar;

            if(_uiState.Faction != null && Entity.TryGetDataBlob<NameDB>(out var nameDB))
            {
                Title = nameDB.GetName(_uiState.Faction);
            }
            else
            {
                Title = "Unknown";
            }
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
            return EntityState.BodyType switch
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

        private Vector2 CalculateWindowPosition()
        {
            var viewportSize = _uiState.ViewPort.Size;

            // Final position: bottom right corner
            float finalX = viewportSize.Width - WindowWidth - RightMargin;
            float finalY = viewportSize.Height - WindowHeight - BottomMargin;

            // Animate from right (offscreen beyond right edge) into final position
            // When progress is 0, window is offscreen to the right
            // When progress is 1, window is at its final position
            float startX = viewportSize.Width; // Start completely off-screen to the right
            float currentX = startX + (finalX - startX) * _animationProgress;

            return new Vector2(currentX, finalY);
        }

        internal override void Display()
        {
            if(!IsActive && _animationState == AnimationState.Closed) return;

            UpdateAnimation();

            // Don't render if fully closed
            if (_animationState == AnimationState.Closed) return;

            var windowPos = CalculateWindowPosition();
            ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
            ImGui.SetNextWindowSize(new Vector2(WindowWidth, WindowHeight), ImGuiCond.Always);
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
            if (Window.Begin(Title + " (" + EntityState.BodyType.ToDescription() + ")" + "###" + Entity.Id, ref windowOpen, _flags))
            {
                _accentColor = accentColor;
                DrawWindowAccents(accentColor);
                DisplayHeader(accentColor);
                DisplayContent();
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
            ImGui.PushID(EntityState.Id);
            ImGui.PushStyleColor(ImGuiCol.Button, Styles.InvisibleColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered,
                new Vector4(accentColor.X * 0.3f, accentColor.Y * 0.3f, accentColor.Z * 0.3f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive,
                new Vector4(accentColor.X * 0.4f, accentColor.Y * 0.4f, accentColor.Z * 0.4f, 0.7f));
            if (ImGui.ImageButton("##headerpin", _uiState.Img_Pin().ToTextureRef(), new Vector2(pinBtnSize, pinBtnSize)))
            {
                _uiState.Camera.PinToEntity(Entity);
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


            // Ensure cursor is past the header background
            float headerLocalBottom = startLocalY + headerContentHeight + headerPad * 2;
            if (ImGui.GetCursorPosY() < headerLocalBottom)
                ImGui.SetCursorPosY(headerLocalBottom);
        }

        private string GetEntitySubtitle()
        {
            if (Entity.TryGetDataBlob<ShipInfoDB>(out var shipInfo))
                return shipInfo.Design.Name;
            if (Entity.TryGetDataBlob<StarInfoDB>(out var starInfo))
                return starInfo.Class;
            if (Entity.TryGetDataBlob<SystemBodyInfoDB>(out var bodyInfo))
            {
                if (Entity.TryGetDataBlob<PositionDB>(out var positionDB) && positionDB.Parent != null)
                {
                    int factionId = _uiState.Faction?.Id ?? Game.NeutralFactionId;
                    return "Orbiting: " + positionDB.Parent.GetName(factionId);
                }
                return bodyInfo.BodyType.ToDescription();
            }
            return "";
        }

        private void DisplayContent()
        {
            if (_uiState.Game == null || _uiState.Faction == null || Entity.Manager == null)
                return;

            switch (EntityState.BodyType)
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

        // --- Shared Helpers ---

        private void DisplayOrbitInfo()
        {
            if (!Entity.TryGetDataBlob<PositionDB>(out var positionDB)) return;
            Entity? parent = positionDB.Parent;
            if (parent == null) return;

            ImGui.Columns(2, "##orbit-info", true);
            if (Entity.TryGetDataBlob<WarpMovingDB>(out var movedb))
            {
                DisplayHelpers.PrintRow("Warping", Stringify.Velocity(movedb.CurrentNonNewtonionVectorMS.Length()));
            }
            else
            {
                DisplayHelpers.PrintFormattedCell("Orbiting");
                if (ImGui.SmallButton(parent.GetName(_uiState.Faction.Id)))
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
            if (Entity.Manager == null) return;

            foreach (var db in Entity.Manager.GetAllDataBlobsForEntity(Entity.Id))
            {
                if (db is not OrderableDB orderableDB) continue;
                if (orderableDB.ActionList.Count == 0) continue;

                if (ImGui.CollapsingHeader("Orders", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    if (ImGui.BeginTable("OrdersTable", 3, Styles.TableFlags))
                    {
                        ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthStretch, 0.1f);
                        ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthStretch, 0.2f);
                        ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch, 0.7f);
                        ImGui.TableHeadersRow();

                        var actions = orderableDB.ActionList.ToArray();
                        for (int i = 0; i < actions.Length; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.Text((i + 1).ToString());
                            ImGui.TableNextColumn();
                            ImGui.Text(actions[i].Name);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text("IsRunning: " + actions[i].IsRunning);
                                ImGui.Text("IsFinished: " + actions[i].GetIsFinished);
                                ImGui.EndTooltip();
                            }
                            ImGui.TableNextColumn();
                            ImGui.Text(actions[i].Details);
                        }

                        ImGui.EndTable();
                    }
                }
                return;
            }
        }

        private void DisplaySurveyInfo()
        {
            if (Entity.TryGetDataBlob<JPSurveyableDB>(out var jPSurveyableDB))
            {
                Displays.GravitationalAnomlay(_uiState, jPSurveyableDB);
            }
        }

        private void DisplayProgressIndicator()
        {
            bool hasGeoSurvey = Entity.HasDataBlob<GeoSurveyableDB>();
            bool isColonizeable = Entity.HasDataBlob<ColonizeableDB>();
            bool hasColony = TryGetColonyEntity(out var colony);

            // Once a colony on this body has infrastructure installed it's an established,
            // working colony — the survey/colonize progress is behind us, so show a live
            // infrastructure overview in place of the progress bar.
            if (hasColony && TryGetInstalledInfrastructure(colony, out var infrastructure))
            {
                DisplayInfrastructureOverview(colony, infrastructure);
                return;
            }

            if (!hasGeoSurvey && !isColonizeable && !hasColony)
                return;

            int factionId = _uiState.Faction?.Id ?? Game.NeutralFactionId;
            var stages = new System.Collections.Generic.List<SurveyProgressBar.Stage>(3);

            stages.Add(new SurveyProgressBar.Stage(
                "Discovered",
                1f,
                "Discovered\nThis body has been detected and is visible on the system map."));

            if (hasGeoSurvey)
            {
                var geo = Entity.GetDataBlob<GeoSurveyableDB>();
                const string rewardSummary =
                    "Reveals:\n"
                    + "  • Atmospheric composition\n"
                    + "  • Mineral deposits and their accessibility\n"
                    + "  • Surface conditions used to assess colonization";
                float fill = 0f;
                string tooltip;
                if (geo.PointsRequired > 0 && geo.GeoSurveyStatus.ContainsKey(factionId))
                {
                    uint remaining = geo.GeoSurveyStatus[factionId];
                    fill = 1f - (float)remaining / geo.PointsRequired;
                    uint completed = geo.PointsRequired - remaining;
                    if (remaining == 0)
                    {
                        tooltip = "Geological Survey\nComplete. Mineral and atmospheric data are available below.";
                    }
                    else
                    {
                        tooltip = "Geological Survey\nIn progress: " + (fill * 100f).ToString("0") + "%"
                            + " (" + completed + " / " + geo.PointsRequired + " survey points)\n\n"
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
            if (isColonizeable || hasColony)
            {
                string infraTooltip = hasColony
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

        /// <summary>
        /// Resolves the colony associated with this body — either this entity itself, or a
        /// colony orbiting it as a direct child. Mirrors <see cref="EntityExtensions.IsOrHasColony"/>
        /// but hands back the entity so callers can read its DataBlobs directly.
        /// </summary>
        private bool TryGetColonyEntity(out Entity colony)
        {
            if (Entity.HasDataBlob<ColonyInfoDB>())
            {
                colony = Entity;
                return true;
            }

            if (Entity.TryGetDataBlob<PositionDB>(out var positionDB))
            {
                foreach (var child in positionDB.Children)
                {
                    if (child.HasDataBlob<ColonyInfoDB>())
                    {
                        colony = child;
                        return true;
                    }
                }
            }

            colony = Entity.InvalidEntity;
            return false;
        }

        /// <summary>
        /// True when the colony has at least one infrastructure installation. Checks the
        /// installations directly (not just <see cref="InfrastructureDB.CapacityProvided"/>),
        /// so infrastructure that's present but disabled by gravity/pressure tolerance still counts.
        /// </summary>
        private bool TryGetInstalledInfrastructure(Entity colony, out InfrastructureDB infrastructure)
        {
            infrastructure = null!;
            return colony.TryGetDataBlob<InfrastructureDB>(out infrastructure)
                && colony.TryGetDataBlob<ComponentInstancesDB>(out var instances)
                && instances.TryGetComponentsByAttribute<InfrastructureCapacityAtb>(out var components)
                && components.Count > 0;
        }

        private void DisplayInfrastructureOverview(Entity colony, InfrastructureDB infrastructure)
        {
            bool overCapacity = infrastructure.CapacityAvailable < 0;

            int factionId = _uiState.Faction?.Id ?? Game.NeutralFactionId;
            string colonyName = colony.GetName(factionId);
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

        private void StatBlock(string label, string value, Vector4? valueColor = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextUnformatted(label);
            ImGui.PopStyleColor();
            if (valueColor.HasValue)
                ImGui.PushStyleColor(ImGuiCol.Text, valueColor.Value);
            ImGui.TextUnformatted(value);
            if (valueColor.HasValue)
                ImGui.PopStyleColor();
        }

        /// <summary>
        /// Renders label/value stats as accent-tinted cards laid out three per row.
        /// Each card matches the infrastructure overview card's styling.
        /// </summary>
        private void DisplayStatCards(string idPrefix, System.Collections.Generic.List<(string Label, string Value)> stats)
        {
            if (stats.Count == 0) return;

            const int columns = 3;
            const float cardPadding = 6f;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float avail = ImGui.GetContentRegionAvail().X;
            float cardWidth = MathF.Floor((avail - spacing * (columns - 1)) / columns);
            float cardHeight = cardPadding * 2f + ImGui.GetTextLineHeightWithSpacing() * 2f;

            ImGui.PushStyleColor(ImGuiCol.ChildBg,
                new Vector4(_accentColor.X, _accentColor.Y, _accentColor.Z, 0.06f));
            ImGui.PushStyleColor(ImGuiCol.Border,
                new Vector4(_accentColor.X, _accentColor.Y, _accentColor.Z, 0.35f));
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 4f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(cardPadding, cardPadding));

            for (int i = 0; i < stats.Count; i++)
            {
                if (i % columns != 0)
                    ImGui.SameLine();

                if (ImGui.BeginChild(idPrefix + i, new Vector2(cardWidth, cardHeight),
                    ImGuiChildFlags.Borders, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                    ImGui.TextUnformatted(stats[i].Label);
                    ImGui.PopStyleColor();
                    ImGui.TextUnformatted(stats[i].Value);
                }
                ImGui.EndChild();
            }

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);
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
            string extraTooltip = null)
        {
            var dimColor = new Vector4(
                _accentColor.X * 0.3f, _accentColor.Y * 0.3f, _accentColor.Z * 0.3f, 0.4f);
            var healthColor = isPlaceholder ? Styles.DescriptiveColor : GetHealthColor(value);
            var textColor = isPlaceholder ? Styles.DescriptiveColor : healthColor;

            // Background ring (full circle, dim)
            drawList.AddCircle(center, radius, ImGui.ColorConvertFloat4ToU32(dimColor), 32, ringThickness);

            // Foreground arc (starts at 12 o'clock, sweeps clockwise)
            if (!isPlaceholder && value > 0f)
            {
                float startAngle = -MathF.PI / 2f;
                float endAngle = startAngle + value * 2f * MathF.PI;
                drawList.PathArcTo(center, radius, startAngle, endAngle, 32);
                drawList.PathStroke(ImGui.ColorConvertFloat4ToU32(healthColor), ImDrawFlags.None, ringThickness);
            }

            // Center text (centered in the ring)
            var centerTextSize = ImGui.CalcTextSize(centerText);
            drawList.AddText(
                new Vector2(center.X - centerTextSize.X * 0.5f, center.Y - centerTextSize.Y * 0.5f),
                ImGui.ColorConvertFloat4ToU32(textColor),
                centerText);

            // Label below the ring
            var labelSize = ImGui.CalcTextSize(label);
            drawList.AddText(
                new Vector2(center.X - labelSize.X * 0.5f, center.Y + radius + ringThickness * 0.5f + 2f),
                ImGui.ColorConvertFloat4ToU32(Styles.DescriptiveColor),
                label);

            // Tooltip on hover
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

            // Compute values
            float htkValue = 0f;
            string htkText = "-";
            float compValue = 0f;
            string compText = "-";
            float armorValue = 0f;
            string armorText = "N/A";
            bool armorPlaceholder = true;

            if (Entity.TryGetDataBlob<ComponentInstancesDB>(out var compDB))
            {
                float totalHealth = 0f;
                int totalCount = 0;
                int operationalCount = 0;

                foreach (var (designId, instances) in compDB.ComponentsByDesign)
                {
                    foreach (var instance in instances)
                    {
                        totalHealth += instance.HealthPercent;
                        totalCount++;
                        if (instance.HealthPercent > instance.StopWorkingAtPercent && instance.IsEnabled)
                            operationalCount++;
                    }
                }

                if (totalCount > 0)
                {
                    htkValue = totalHealth / totalCount;
                    htkText = (htkValue * 100f).ToString("0") + "%";
                    compValue = (float)operationalCount / totalCount;
                    compText = operationalCount + "/" + totalCount;
                }
            }

            if (Entity.TryGetDataBlob<EntityDamageProfileDB>(out var damageDB))
            {
                if (damageDB.Armor.thickness > 0)
                {
                    armorPlaceholder = false;
                    armorValue = 1.0f;
                    armorText = damageDB.Armor.thickness.ToString("0.#") + "mm";
                }
            }

            // Compute delta V values
            float dvValue = 0f;
            string dvText = "N/A";
            string dvTooltip = null;
            bool dvPlaceholder = true;

            Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrustDB);
            if (thrustDB != null && thrustDB.ExhaustVelocity > 0)
            {
                dvPlaceholder = false;
                double dv = thrustDB.DeltaV;
                dvTooltip = Stringify.Velocity(dv);

                // Compact center text
                if (dv >= 1e6)
                    dvText = (dv / 1e6).ToString("0.#") + "M";
                else if (dv >= 1e3)
                    dvText = (dv / 1e3).ToString("0.#") + "k";
                else
                    dvText = dv.ToString("0");

                // Compute percentage: current DV / max DV at full fuel
                if (dv > 0 &&
                    Entity.TryGetDataBlob<MassVolumeDB>(out var massDB) &&
                    Entity.TryGetDataBlob<CargoStorageDB>(out var cargoStorage))
                {
                    // FuelType is a material UniqueID, not a cargo type key.
                    // Look up the fuel ICargoable to find which TypeStore it lives in.
                    var cargoLib = Entity.GetFactionCargoDefinitions();
                    if (cargoLib != null && cargoLib.Contains(thrustDB.FuelType))
                    {
                        var fuelCargoable = cargoLib.GetAny(thrustDB.FuelType);
                        if (fuelCargoable != null &&
                            cargoStorage.TypeStores.ContainsKey(fuelCargoable.CargoTypeID) &&
                            fuelCargoable.VolumePerUnit > 0)
                        {
                            var fuelStore = cargoStorage.TypeStores[fuelCargoable.CargoTypeID];
                            double maxFuelUnits = fuelStore.MaxVolume / fuelCargoable.VolumePerUnit;
                            double maxFuel_kg = maxFuelUnits * fuelCargoable.MassPerUnit;

                            double dryMass = massDB.MassTotal - thrustDB.TotalFuel_kg;
                            if (dryMass > 0 && maxFuel_kg > 0)
                            {
                                double maxWetMass = dryMass + maxFuel_kg;
                                double maxDV = thrustDB.ExhaustVelocity * Math.Log(maxWetMass / dryMass);
                                if (maxDV > 0)
                                    dvValue = Math.Clamp((float)(dv / maxDV), 0f, 1f);
                            }
                        }
                    }
                }
            }

            // Draw five indicators left-aligned
            float x0 = cursorPos.X + radius;

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

            // Current order (right-aligned on the same row)
            string orderLabel = "CURRENT ORDER";
            string orderName = "Idle";
            string orderDetails = "";

            if (Entity.Manager != null)
            {
                foreach (var db in Entity.Manager.GetAllDataBlobsForEntity(Entity.Id))
                {
                    if (db is not OrderableDB orderableDB) continue;
                    if (orderableDB.ActionList.Count == 0) continue;

                    var current = orderableDB.ActionList.ToArray()[0];
                    orderName = current.Name;
                    orderDetails = current.Details;
                    break;
                }
            }

            float rightEdge = cursorPos.X + availWidth;
            var labelSize = ImGui.CalcTextSize(orderLabel);
            var nameSize = ImGui.CalcTextSize(orderName);

            // Right-align: find the widest text to anchor from
            float maxTextWidth = Math.Max(labelSize.X, nameSize.X);
            if (orderDetails.Length > 0)
            {
                var detailSize = ImGui.CalcTextSize(orderDetails);
                maxTextWidth = Math.Max(maxTextWidth, detailSize.X);
            }
            float textX = rightEdge - maxTextWidth;

            // Label
            float textY = cursorPos.Y + 4f;
            drawList.AddText(
                new Vector2(textX, textY),
                ImGui.ColorConvertFloat4ToU32(Styles.DescriptiveColor),
                orderLabel);

            // Order name
            float nameY = textY + labelSize.Y + 2f;
            var nameColor = orderDetails.Length > 0 ? _accentColor : Styles.NeutralColor;
            drawList.AddText(
                new Vector2(textX, nameY),
                ImGui.ColorConvertFloat4ToU32(nameColor),
                orderName);

            // Order details (if any)
            if (orderDetails.Length > 0)
            {
                float detailY = nameY + nameSize.Y + 1f;
                drawList.AddText(
                    new Vector2(textX, detailY),
                    ImGui.ColorConvertFloat4ToU32(Styles.DescriptiveColor),
                    orderDetails);
            }

            // Reserve vertical space for the indicator row
            ImGui.InvisibleButton("##statusRow", new Vector2(availWidth, radius * 2f + 24f));
        }

        // --- Type-Specific Content ---

        private void DisplayShipContent()
        {
            Entity.TryGetDataBlob<ShipInfoDB>(out var shipInfo);

            DisplayShipStatusRow();

            // Crew row
            SectionLabel("CREW");

            string captainName = "Unassigned";
            if (shipInfo != null && shipInfo.CommanderID >= 0 && Entity.Manager != null
                && Entity.Manager.TryGetEntityById(shipInfo.CommanderID, out var cmdEntity)
                && cmdEntity.TryGetDataBlob<CommanderDB>(out var cmdDB))
            {
                captainName = cmdDB.Name;
            }

            ImGui.Indent();
            int crewCols = shipInfo != null ? 2 : 1;
            if (ImGui.BeginTable("##crew", crewCols, ImGuiTableFlags.SizingStretchSame))
            {
                ImGui.TableNextColumn();
                StatBlock("COMMANDER", captainName);

                if (shipInfo != null)
                {
                    ImGui.TableNextColumn();
                    StatBlock("CREW", shipInfo.Design.CrewReq.ToString());
                }

                ImGui.EndTable();
            }
            ImGui.Unindent();

            // Propulsion stat grid
            Entity.TryGetDataBlob<NewtonThrustAbilityDB>(out var thrustDB);
            Entity.TryGetDataBlob<WarpAbilityDB>(out var warpDB);

            if (thrustDB != null || warpDB != null)
            {
                SectionLabel("PROPULSION");

                ImGui.Indent();
                int propCols = (thrustDB != null ? 3 : 0) + (warpDB != null ? 1 : 0);
                if (ImGui.BeginTable("##propulsion", propCols, ImGuiTableFlags.SizingStretchSame))
                {
                    if (thrustDB != null)
                    {
                        ImGui.TableNextColumn();
                        StatBlock("THRUST", Stringify.Thrust(thrustDB.ThrustInNewtons));

                        ImGui.TableNextColumn();
                        StatBlock("BURN", Stringify.Mass(thrustDB.FuelBurnRate) + "/s");

                        ImGui.TableNextColumn();
                        StatBlock("EXHAUST", Stringify.Velocity(thrustDB.ExhaustVelocity));
                    }
                    if (warpDB != null)
                    {
                        ImGui.TableNextColumn();
                        StatBlock("WARP", Stringify.Velocity(warpDB.MaxSpeed));
                    }
                    ImGui.EndTable();
                }
                ImGui.Unindent();
            }

            // Location
            if (Entity.TryGetDataBlob<PositionDB>(out var positionDB) && positionDB.Parent != null)
            {
                SectionLabel("LOCATION");
                ImGui.Indent();
                ImGui.PushStyleColor(ImGuiCol.Text, _accentColor);
                if (Entity.TryGetDataBlob<WarpMovingDB>(out var movedb))
                {
                    ImGui.TextUnformatted("Warping");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    ImGui.TextUnformatted(Stringify.Velocity(movedb.CurrentNonNewtonionVectorMS.Length()));
                }
                else
                {
                    ImGui.TextUnformatted("Orbiting");
                    ImGui.PopStyleColor();
                    ImGui.SameLine();
                    if (ImGui.SmallButton(positionDB.Parent.GetName(_uiState.Faction.Id)))
                    {
                        _uiState.EntityClicked(positionDB.Parent.Id, _uiState.SelectedStarSystemId, MouseButtons.Primary);
                    }
                }
                ImGui.Unindent();
            }

            // Orders (inline, no collapsing header)
            if (Entity.Manager != null)
            {
                foreach (var db in Entity.Manager.GetAllDataBlobsForEntity(Entity.Id))
                {
                    if (db is not OrderableDB orderableDB) continue;
                    if (orderableDB.ActionList.Count == 0) continue;

                    SectionLabel("ORDERS (" + orderableDB.ActionList.Count + ")");

                    ImGui.Indent();
                    if (ImGui.BeginTable("##orders", 3,
                        ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoPadOuterX))
                    {
                        ImGui.TableSetupColumn("##n", ImGuiTableColumnFlags.WidthFixed, 20f);
                        ImGui.TableSetupColumn("##cmd", ImGuiTableColumnFlags.WidthFixed, 100f);
                        ImGui.TableSetupColumn("##det", ImGuiTableColumnFlags.WidthStretch);

                        var actions = orderableDB.ActionList.ToArray();
                        for (int i = 0; i < actions.Length; i++)
                        {
                            ImGui.TableNextColumn();
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.Text((i + 1).ToString());
                            ImGui.PopStyleColor();
                            ImGui.TableNextColumn();

                            // Make NewtonThrustCommand orders clickable for editing
                            if (actions[i] is NewtonThrustCommand thrustCmd && !thrustCmd.IsRunning)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Header, Styles.InvisibleColor);
                                ImGui.PushStyleColor(ImGuiCol.HeaderHovered,
                                    new Vector4(_accentColor.X * 0.2f, _accentColor.Y * 0.2f, _accentColor.Z * 0.2f, 0.5f));
                                ImGui.PushStyleColor(ImGuiCol.HeaderActive,
                                    new Vector4(_accentColor.X * 0.3f, _accentColor.Y * 0.3f, _accentColor.Z * 0.3f, 0.7f));
                                if (ImGui.Selectable(actions[i].Name + "##order" + i, false, ImGuiSelectableFlags.SpanAllColumns))
                                {
                                    _uiState.OpenManeuverPanelForOrder(Entity, thrustCmd);
                                }
                                ImGui.PopStyleColor(3);
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Click to edit or delete this order");
                                    ImGui.EndTooltip();
                                }
                            }
                            else
                            {
                                ImGui.Text(actions[i].Name);
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.Text("Running: " + actions[i].IsRunning);
                                    ImGui.Text("Finished: " + actions[i].GetIsFinished);
                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.TableNextColumn();
                            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                            ImGui.Text(actions[i].Details);
                            ImGui.PopStyleColor();
                        }
                        ImGui.EndTable();
                    }
                    ImGui.Unindent();
                    break;
                }
            }

            // Cargo summary bars
            if (Entity.TryGetDataBlob<CargoStorageDB>(out var storage) && storage.TypeStores.Count > 0)
            {
                SectionLabel("CARGO");

                ImGui.Indent();
                if (Entity.GetFactionOwner.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
                {
                    foreach (var (sid, storageType) in storage.TypeStores)
                    {
                        string name = factionInfoDB.Data.CargoTypes[sid].Name;
                        double freeVolume = storage.GetFreeVolume(sid);
                        double usedVolume = storageType.MaxVolume - freeVolume;
                        double percent = storageType.MaxVolume > 0 ? usedVolume / storageType.MaxVolume : 0;

                        string barLabel = name + "  " + (percent * 100).ToString("0") + "%  ·  " +
                            Stringify.VolumeLtr(usedVolume) + " / " + Stringify.VolumeLtr(storageType.MaxVolume);

                        Vector4 barColor = new Vector4(
                            _accentColor.X * 0.4f, _accentColor.Y * 0.4f, _accentColor.Z * 0.4f, 0.8f);
                        if (percent > 0.9)
                            barColor = Styles.BadColor;
                        else if (percent > 0.75)
                            barColor = Styles.OkColor;

                        ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.08f, 0.08f, 0.1f, 0.5f));
                        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
                        ImGui.ProgressBar((float)percent, new Vector2(ImGui.GetContentRegionAvail().X, 16), barLabel);
                        ImGui.PopStyleColor(2);
                    }
                }
                ImGui.Unindent();
            }
        }

        private void DisplayStarContent()
        {
            Entity.TryGetDataBlob<StarInfoDB>(out var starInfo);
            Entity.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB);

            ImGui.Columns(2, "##star-info", true);

            if (starInfo != null)
            {
                DisplayHelpers.PrintRow("Spectral Type", starInfo.SpectralType.ToDescription() + starInfo.SpectralSubDivision);
                DisplayHelpers.PrintRow("Class", starInfo.Class);
                DisplayHelpers.PrintRow("Temperature", starInfo.Temperature.ToString("#,##0") + " °C");
                DisplayHelpers.PrintRow("Luminosity", starInfo.Luminosity + " " + starInfo.LuminosityClass.ToString() + " (" + starInfo.LuminosityClass.ToDescription() + ")");
                DisplayHelpers.PrintRow("Age", Stringify.Quantity(starInfo.Age));
            }

            if (massVolumeDB != null)
            {
                DisplayHelpers.PrintRow("Mass", Stringify.CelestialMass(massVolumeDB.MassTotal));
                DisplayHelpers.PrintRow("Radius", Stringify.Distance(massVolumeDB.RadiusInM));
                DisplayHelpers.PrintRow("Density", massVolumeDB.DensityDry_gcm.ToString("##0.000") + " g/cm³");
            }

            if (starInfo != null)
            {
                DisplayHelpers.PrintRow("Habitable Zone", starInfo.MinHabitableRadius_AU.ToString("0.##") + " - " + starInfo.MaxHabitableRadius_AU.ToString("0.##") + " AU");
            }

            ImGui.Columns(1);

            DisplayOrbitInfo();
            DisplaySurveyInfo();
        }

        private void DisplaySystemBodyContent()
        {
            bool isGeoSurveyed = Entity.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB)
                && geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id);

            DisplayProgressIndicator();

            var bodyStats = new System.Collections.Generic.List<(string Label, string Value)>(10);
            bool hasBodyInfo = Entity.TryGetDataBlob<SystemBodyInfoDB>(out var bodyInfo);
            if (hasBodyInfo)
            {
                bodyStats.Add(("Gravity",
                    bodyInfo.Gravity.ToString("0.##") + " m/s² · "
                    + (bodyInfo.Gravity / 9.80665).ToString("0.###") + " G"));
                bodyStats.Add(("Temperature", bodyInfo.BaseTemperature.ToString("##0.#") + " °C"));
                bodyStats.Add(("Day Length", bodyInfo.LengthOfDay.TotalDays.ToString("0.#") + " days"));
                bodyStats.Add(("Axial Tilt", bodyInfo.AxialTilt.ToString("0.#") + "°"));
                bodyStats.Add(("Tectonics", bodyInfo.Tectonics.ToString()));
                bodyStats.Add(("Magnetic Field", bodyInfo.MagneticField.ToString("0.##") + " μT"));
                // Every colony needs infrastructure now, so show what a body's infrastructure
                // must be rated for. Earth-like worlds take the default Earth-Standard design;
                // hostile worlds need one tuned to their gravity and atmospheric pressure.
                string infraReq;
                if (!Entity.HasDataBlob<ColonizeableDB>())
                {
                    infraReq = "Not colonizable";
                }
                else if (bodyInfo.SupportsPopulations)
                {
                    infraReq = "Earth-Standard";
                }
                else
                {
                    string grav = bodyInfo.Gravity.ToString("0.##") + " m/s²";
                    string pressure;
                    if (!isGeoSurveyed)
                        pressure = "? atm"; // atmospheric pressure isn't known until surveyed
                    else if (Entity.TryGetDataBlob<AtmosphereDB>(out var atmo) && atmo.Pressure > 0)
                        pressure = atmo.Pressure.ToString("0.##") + " atm";
                    else
                        pressure = "vacuum";
                    infraReq = grav + " · " + pressure;
                }
                bodyStats.Add(("Infrastructure", infraReq));
            }

            if (Entity.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB))
            {
                bodyStats.Add(("Mass", Stringify.CelestialMass(massVolumeDB.MassTotal)));
                bodyStats.Add(("Radius", Stringify.Distance(massVolumeDB.RadiusInM)));
            }

            SectionLabel(hasBodyInfo ? bodyInfo.BodyType.ToDescription().ToUpperInvariant() : "CELESTIAL BODY");
            DisplayStatCards("##body-stat", bodyStats);

            if (isGeoSurveyed && Entity.TryGetDataBlob<AtmosphereDB>(out var atmosphereDB))
            {
                atmosphereDB.Display(EntityState, _uiState);
            }

            if (Entity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfoDB))
            {
                colonyInfoDB.Display(EntityState, _uiState);
            }

            if (isGeoSurveyed && Entity.TryGetDataBlob<MineralsDB>(out var mineralsDB)
                && ImGui.CollapsingHeader("Minerals", ImGuiTreeNodeFlags.DefaultOpen))
            {
                mineralsDB.Display(EntityState, _uiState);
            }

            DisplaySurveyInfo();

            if (Entity.CanShowMiningTab() && ImGui.CollapsingHeader("Mining", ImGuiTreeNodeFlags.DefaultOpen))
            {
                Entity.DisplayMining(_uiState);
            }

            // Installations (collapsed by default)
            if (Entity.TryGetDataBlob<ComponentInstancesDB>(out var compDB)
                && ImGui.CollapsingHeader("Installations"))
            {
                compDB.Display(EntityState, _uiState);
            }

            // Cargo (collapsed by default)
            if (Entity.TryGetDataBlob<CargoStorageDB>(out var cargoDB))
            {
                cargoDB.Display(EntityState, _uiState, ImGuiTreeNodeFlags.None);
            }
        }

        private void DisplaySmallBodyContent()
        {
            bool isGeoSurveyed = Entity.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB)
                && geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id);

            DisplayProgressIndicator();

            ImGui.Columns(2, "##small-body-info", true);

            if (Entity.TryGetDataBlob<SystemBodyInfoDB>(out var bodyInfo))
            {
                DisplayHelpers.PrintRow("Body Type", bodyInfo.BodyType.ToDescription());
            }

            if (Entity.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB))
            {
                DisplayHelpers.PrintRow("Mass", Stringify.CelestialMass(massVolumeDB.MassTotal));
                DisplayHelpers.PrintRow("Radius", Stringify.Distance(massVolumeDB.RadiusInM));
            }

            ImGui.Columns(1);

            if (isGeoSurveyed && Entity.TryGetDataBlob<MineralsDB>(out var mineralsDB)
                && ImGui.CollapsingHeader("Minerals", ImGuiTreeNodeFlags.DefaultOpen))
            {
                mineralsDB.Display(EntityState, _uiState);
            }

            DisplaySurveyInfo();
        }

        private void DisplayColonyContent()
        {
            bool isGeoSurveyed = Entity.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB)
                && geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id);

            DisplayProgressIndicator();

            // Population (prominent at top)
            if (Entity.TryGetDataBlob<ColonyInfoDB>(out var colonyInfoDB))
            {
                colonyInfoDB.Display(EntityState, _uiState);
            }

            // Environment section
            if (ImGui.CollapsingHeader("Environment"))
            {
                ImGui.Columns(2, "##environment-info", true);
                if (Entity.TryGetDataBlob<SystemBodyInfoDB>(out var bodyInfo))
                {
                    DisplayHelpers.PrintRow("Body Type", bodyInfo.BodyType.ToDescription());
                    DisplayHelpers.PrintRow("Gravity", bodyInfo.Gravity.ToString("0.##") + " m/s²",
                        null, (bodyInfo.Gravity / 9.80665).ToString("0.###") + " G");
                    DisplayHelpers.PrintRow("Temperature", bodyInfo.BaseTemperature.ToString("##0.#") + " °C");
                }
                if (Entity.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB))
                {
                    DisplayHelpers.PrintRow("Radius", Stringify.Distance(massVolumeDB.RadiusInM));
                }
                ImGui.Columns(1);
            }

            // Atmosphere
            if (isGeoSurveyed && Entity.TryGetDataBlob<AtmosphereDB>(out var atmosphereDB))
            {
                atmosphereDB.Display(EntityState, _uiState);
            }

            // Minerals (collapsed by default)
            if (isGeoSurveyed && Entity.TryGetDataBlob<MineralsDB>(out var mineralsDB)
                && ImGui.CollapsingHeader("Minerals"))
            {
                mineralsDB.Display(EntityState, _uiState);
            }

            // Mining
            if (Entity.CanShowMiningTab() && ImGui.CollapsingHeader("Mining", ImGuiTreeNodeFlags.DefaultOpen))
            {
                Entity.DisplayMining(_uiState);
            }

            // Installations
            if (Entity.TryGetDataBlob<ComponentInstancesDB>(out var compDB)
                && ImGui.CollapsingHeader("Installations", ImGuiTreeNodeFlags.DefaultOpen))
            {
                compDB.Display(EntityState, _uiState);
            }

            // Cargo
            if (Entity.TryGetDataBlob<CargoStorageDB>(out var cargoDB))
            {
                cargoDB.Display(EntityState, _uiState);
            }

            DisplayOrbitInfo();
        }

        private void DisplayGenericContent()
        {
            ImGui.Columns(2, "##generic-info", true);
            if (Entity.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB))
            {
                DisplayHelpers.PrintRow("Mass", Stringify.Mass(massVolumeDB.MassTotal));
                DisplayHelpers.PrintRow("Radius", Stringify.Distance(massVolumeDB.RadiusInM));
            }
            ImGui.Columns(1);

            DisplayOrbitInfo();
            DisplayOrders();

            if (Entity.TryGetDataBlob<ComponentInstancesDB>(out var compDB)
                && ImGui.CollapsingHeader("Components", ImGuiTreeNodeFlags.DefaultOpen))
            {
                compDB.Display(EntityState, _uiState);
            }

            if (Entity.TryGetDataBlob<CargoStorageDB>(out var cargoDB))
            {
                cargoDB.Display(EntityState, _uiState);
            }

            bool isGeoSurveyed = Entity.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB)
                && geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id);

            if (isGeoSurveyed && Entity.TryGetDataBlob<MineralsDB>(out var mineralsDB)
                && ImGui.CollapsingHeader("Minerals", ImGuiTreeNodeFlags.DefaultOpen))
            {
                mineralsDB.Display(EntityState, _uiState);
            }

            DisplaySurveyInfo();
        }
    }
}
