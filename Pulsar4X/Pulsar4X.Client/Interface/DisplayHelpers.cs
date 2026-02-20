using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;
using Pulsar4X.Ships;
using Pulsar4X.Technology;
using Pulsar4X.Factions;
using Pulsar4X.People;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Client
{
    public static class DisplayHelpers
    {
        private static Dictionary<string, int> _peopleChooserSelections = new();

        /// <summary>
        /// Creates an ImTextureRef from an IntPtr texture ID for use with ImGui.Image and ImGui.ImageButton.
        /// </summary>
        public static unsafe ImTextureRef ToTextureRef(this IntPtr textureId)
        {
            return new ImTextureRef { _TexData = null, _TexID = textureId };
        }
        public static void Header(string text, string? tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(text);
            if(!string.IsNullOrEmpty(tooltip))
            {
                ImGui.SameLine();
                ImGui.Text("[?]");
                if(ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);
            }
            ImGui.PopStyleColor();
            ImGui.Separator();
        }

        public static void PrintRow(string one, string two, string? tooltipOne = null, string? tooltipTwo = null, bool separator = true)
        {
            PrintFormattedCell(one, tooltipOne);
            PrintCell(two, tooltipTwo);

            if(separator)
                ImGui.Separator();
        }

        public static void PrintFormattedCell(string text, string? tooltip = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.Text(text);
            ImGui.PopStyleColor();
            if(!string.IsNullOrEmpty(tooltip))
            {
                if(ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            }
            ImGui.NextColumn();
        }

        public static void PrintCell(string text, string? tooltip = null)
        {
            ImGui.Text(text);
            if(!string.IsNullOrEmpty(tooltip))
            {
                if(ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            }
            ImGui.NextColumn();
        }

        public static void ShipTooltip(Entity ship, int factionId)
        {
            if(!ship.TryGetDataBlob<ShipInfoDB>(out var shipInfo))
                return;

            if(!ship.TryGetDataBlob<OrderableDB>(out var orderableDB))
                return;

            var description = "No orders";
            if(orderableDB.ActionList.Count > 0)
            {
                description = "Orders: ";
                foreach(var action in orderableDB.ActionList)
                {
                    description += action.Name;
                    if(action.IsRunning)
                        description += " (running)";
                    else
                        description += " (not running)";
                }
            }

            var meta = "";
            if(ship.Manager != null && ship.Manager.TryGetEntityById(shipInfo.CommanderID, out var commander))
            {
                meta = "Commanded by: " + commander.GetName(factionId);
            }

            DescriptiveTooltip(ship.GetName(factionId), shipInfo.Design.Name, description, () => ImGui.Text(meta));
        }

        public static void DescriptiveTooltipRaw(string name, string type, string description, Action? callback = null, bool hideTypeIfSameAsName = false, bool hideDescriptionColor = false)
        {
            ImGui.SetNextWindowSize(Styles.ToolTipsize);
            ImGui.BeginTooltip();
            ImGui.Text(Utils.Truncate(name, 32));
            if(type.IsNotNullOrEmpty() && (!hideTypeIfSameAsName || (hideTypeIfSameAsName && !type.Equals(name))))
            {
                var size = ImGui.GetContentRegionAvail();
                var text = Utils.Truncate(type, 21);
                var textSize = ImGui.CalcTextSize(text);
                ImGui.SameLine();
                ImGui.SetCursorPosX(size.X - textSize.X);
                ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                ImGui.Text(text);
                ImGui.PopStyleColor();
            }
            var showDescription = description.IsNotNullOrEmpty();

            if(showDescription || callback != null)
            {
                ImGui.Separator();
            }

            if(!hideDescriptionColor) ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            if(showDescription)
            {
                ImGui.TextWrapped(description);
            }
            callback?.Invoke();
            if(!hideDescriptionColor) ImGui.PopStyleColor();
            ImGui.EndTooltip();
        }

        public static void DescriptiveTooltip(string name, string type, string description, Action? callback = null, bool hideTypeIfSameAsName = false, bool hideDescriptionColor = false)
        {
            if(ImGui.IsItemHovered())
                DescriptiveTooltipRaw(name, type, description, callback, hideTypeIfSameAsName, hideDescriptionColor);
        }

        public static void Indent()
        {
            ImGui.InvisibleButton("", Styles.Indent);
            ImGui.SameLine();
        }

        public static void TechTooltip(Tech tech, GlobalUIState state)
        {
            if (ImGui.IsItemHovered())
            {
                DescriptiveTooltip(
                    tech.Name,
                    state.Game?.TechCategories[tech.Category].Name ?? "Unknown",
                    $"{tech.Description}\n\nProgress: {tech.ResearchProgress}/{tech.ResearchCost}");
            }
        }

        public static int PeopleChooser(GlobalUIState state, int currentlySelectedId, CommanderTypes defaultFilterTypes, string instanceKey = "default", Action? onCancel = null)
        {
            if(state.Faction == null
                || state.Game == null
                || !state.Faction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
                return currentlySelectedId;

            // Track which person is selected in the UI (not yet assigned)
            string selectionKey = $"{instanceKey}_{defaultFilterTypes}";
            if(!_peopleChooserSelections.ContainsKey(selectionKey))
            {
                _peopleChooserSelections[selectionKey] = -1;
            }
            int uiSelectedId = _peopleChooserSelections[selectionKey];

            // Build the list of available commanders
            var availableCommanders = new List<Entity>();
            foreach(var commander in factionInfoDB.Commanders)
            {
                if(!commander.TryGetDataBlob<CommanderDB>(out var cmdDB))
                    continue;

                if(cmdDB.Type != defaultFilterTypes)
                    continue;

                // Don't show the currently assigned person in the list
                if(commander.Id == currentlySelectedId)
                    continue;

                availableCommanders.Add(commander);
            }

            Vector2 contentSize = ImGui.GetContentRegionAvail();
            float leftWidth = 200f;
            float rightWidth = Math.Max(contentSize.X - leftWidth - 8, 400f);
            float panelHeight = Math.Max(300f, contentSize.Y);

            int returnValue = currentlySelectedId;

            // Left side - scrollable list of people
            if(ImGui.BeginChild("PeopleList", new Vector2(leftWidth, panelHeight), ImGuiChildFlags.Borders))
            {
                Header("Available");

                // Option to unassign (select "None")
                if(currentlySelectedId >= 0)
                {
                    bool isNoneSelected = uiSelectedId == 0; // Use 0 as special "None" marker
                    if(ImGui.Selectable("None (Unassign)", isNoneSelected))
                    {
                        _peopleChooserSelections[selectionKey] = 0;
                    }
                }

                foreach(var commander in availableCommanders)
                {
                    if(!commander.TryGetDataBlob<CommanderDB>(out var commanderDB))
                        continue;

                    bool isSelected = uiSelectedId == commander.Id;
                    string name = commander.GetName(state.Faction.Id);

                    // Show assignment status
                    string displayName = name;
                    if(commanderDB.AssignedTo >= 0)
                    {
                        displayName += " *";
                    }

                    if(ImGui.Selectable(displayName + $"###{commander.Id}", isSelected))
                    {
                        _peopleChooserSelections[selectionKey] = commander.Id;
                    }

                    if(ImGui.IsItemHovered() && commanderDB.AssignedTo >= 0)
                    {
                        ImGui.SetTooltip("Currently assigned elsewhere");
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();

            // Right side - details of selected person with buttons at bottom
            if(ImGui.BeginChild("PersonDetails", new Vector2(rightWidth, panelHeight), ImGuiChildFlags.Borders))
            {
                // Calculate space for buttons at bottom
                float buttonHeight = 30f;
                float buttonSpacing = 8f;
                float availableHeight = ImGui.GetContentRegionAvail().Y;
                float detailsHeight = availableHeight - buttonHeight - buttonSpacing;

                // Details section
                if(ImGui.BeginChild("PersonDetailsContent", new Vector2(0, detailsHeight)))
                {
                    if(uiSelectedId == 0)
                    {
                        // "None" selected
                        Header("Unassign");
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.TextWrapped("Select this to remove the current assignment.");
                        ImGui.PopStyleColor();
                    }
                    else if(uiSelectedId > 0)
                    {
                        // Show details about selected commander
                        Entity? selectedCommander = null;
                        foreach(var commander in availableCommanders)
                        {
                            if(commander.Id == uiSelectedId)
                            {
                                selectedCommander = commander;
                                break;
                            }
                        }

                        if(selectedCommander != null && selectedCommander.TryGetDataBlob<CommanderDB>(out var commanderDB))
                        {
                            string name = selectedCommander.GetName(state.Faction.Id);

                            // Portrait and name header with background
                            float portraitSize = 32f;
                            float headerPadding = 4f;
                            float headerHeight = portraitSize + headerPadding * 2;

                            // Draw background rectangle
                            var drawList = ImGui.GetWindowDrawList();
                            Vector2 headerMin = ImGui.GetCursorScreenPos();
                            Vector2 headerMax = new Vector2(
                                headerMin.X + ImGui.GetContentRegionAvail().X,
                                headerMin.Y + headerHeight);
                            uint headerBgColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.25f, 1.0f));
                            drawList.AddRectFilled(headerMin, headerMax, headerBgColor, 4.0f);

                            // Add padding before content
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + headerPadding);
                            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + headerPadding);

                            IntPtr portraitTexture = state.Img_Character();
                            if(portraitTexture != IntPtr.Zero)
                            {
                                ImGui.Image(portraitTexture.ToTextureRef(), new Vector2(portraitSize, portraitSize));
                                ImGui.SameLine();
                            }

                            // Name to the right of portrait, vertically centered
                            float textHeight = ImGui.GetTextLineHeight();
                            float verticalOffset = (portraitSize - textHeight) / 2;
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
                            ImGui.Text(name);

                            // Move cursor past the header area
                            ImGui.SetCursorPosY(headerMin.Y - ImGui.GetWindowPos().Y + headerHeight + headerPadding);

                            ImGui.Columns(2, "PersonDetailsColumns", false);
                            ImGui.SetColumnWidth(0, 120);

                            PrintFormattedCell("Type:");
                            PrintCell(commanderDB.Type.ToString());

                            PrintFormattedCell("Experience:");
                            PrintCell($"{commanderDB.Experience} / {commanderDB.ExperienceCap}");

                            PrintFormattedCell("Commissioned:");
                            PrintCell(commanderDB.CommissionedOn.ToString("yyyy-MM-dd"));

                            if(commanderDB.AssignedTo >= 0)
                            {
                                PrintFormattedCell("Status:");
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
                                PrintCell("Assigned elsewhere");
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                PrintFormattedCell("Status:");
                                ImGui.PushStyleColor(ImGuiCol.Text, Styles.GoodColor);
                                PrintCell("Available");
                                ImGui.PopStyleColor();
                            }

                            ImGui.Columns(1);

                            // Display bonuses if available
                            if(selectedCommander.TryGetDataBlob<BonusesDB>(out var bonusesDB) && bonusesDB.Bonuses.Count > 0)
                            {
                                ImGui.NewLine();
                                Header("Bonuses");

                                var factionData = state.Faction.GetDataBlob<FactionInfoDB>().Data;

                                foreach(var bonus in bonusesDB.Bonuses)
                                {
                                    string valueStr = bonus.Type == BonusType.Perentage
                                        ? $"{bonus.Value * 100:+0.#;-0.#}%"
                                        : $"{bonus.Value:+0.#;-0.#}";

                                    // Build the bonus display text
                                    string bonusText = bonus.Name;
                                    if(!string.IsNullOrEmpty(bonus.FilterId))
                                    {
                                        // Try to resolve the FilterId to a readable name
                                        string filterName = factionData.GetName(bonus.FilterId);

                                        // If not found in faction data, check tech categories
                                        if(string.IsNullOrEmpty(filterName) && state.Game.TechCategories.ContainsKey(bonus.FilterId))
                                        {
                                            filterName = state.Game.TechCategories[bonus.FilterId].Name;
                                        }

                                        // Fall back to the raw ID if nothing resolved
                                        if(string.IsNullOrEmpty(filterName))
                                        {
                                            filterName = bonus.FilterId;
                                        }

                                        bonusText += $" ({filterName})";
                                    }

                                    ImGui.PushStyleColor(ImGuiCol.Text, bonus.Value >= 0 ? Styles.GoodColor : Styles.BadColor);
                                    ImGui.TextUnformatted(valueStr);
                                    ImGui.PopStyleColor();
                                    ImGui.SameLine();
                                    ImGui.Text(bonusText);
                                }
                            }
                        }
                    }
                    else
                    {
                        // No selection yet
                        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                        ImGui.TextWrapped("Select a person from the list to view their details.");
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndChild();

                // Buttons at bottom of right pane
                float availableWidth = ImGui.GetContentRegionAvail().X;
                bool hasCancel = onCancel != null;
                float buttonWidth = hasCancel ? (availableWidth - 8) / 2 : availableWidth;

                bool canAssign = uiSelectedId != -1;
                if(!canAssign)
                {
                    ImGui.BeginDisabled();
                }

                if(ImGui.Button("Assign", new Vector2(buttonWidth, buttonHeight)))
                {
                    returnValue = uiSelectedId == 0 ? -1 : uiSelectedId; // Convert "None" (0) back to -1
                    _peopleChooserSelections[selectionKey] = -1; // Reset selection
                }

                if(!canAssign)
                {
                    ImGui.EndDisabled();
                }

                if(hasCancel)
                {
                    ImGui.SameLine();
                    if(ImGui.Button("Cancel", new Vector2(buttonWidth, buttonHeight)))
                    {
                        _peopleChooserSelections[selectionKey] = -1; // Reset selection
                        onCancel?.Invoke();
                    }
                }
            }
            ImGui.EndChild();

            return returnValue;
        }
    }
}
