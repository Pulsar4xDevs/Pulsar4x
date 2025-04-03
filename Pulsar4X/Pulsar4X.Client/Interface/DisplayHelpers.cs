using System;
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

        public static void DescriptiveTooltip(string name, string type, string description, Action? callback = null, bool hideTypeIfSameAsName = false, bool hideDescriptionColor = false)
        {
            if(ImGui.IsItemHovered())
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

        public static int PeopleChooser(GlobalUIState state, int currentlySelectedId, CommanderTypes defaultFilterTypes)
        {
            if(state.Faction == null
                || state.Game == null
                || !state.Faction.TryGetDataBlob<FactionInfoDB>(out var factionInfoDB))
                return currentlySelectedId;

            foreach(var commanderId in factionInfoDB.Commanders)
            {
                if(commanderId == currentlySelectedId)
                    continue;

                // TODO: this is probably super slow and should be improved
                // TODO: remove the call into the game
                var commander = state.Game.GlobalManager.GetGlobalEntityById(commanderId);

                if(!commander.TryGetDataBlob<CommanderDB>(out var commanderDB))
                    continue;

                if(commanderDB.Type != defaultFilterTypes)
                    continue;

                if(ImGui.Button(commander.GetName(state.Faction.Id)))
                {
                    return commanderId;
                }
            }

            if(currentlySelectedId >= 0 && ImGui.Button("None"))
            {
                // -1 to indicate no commander selected
                return -1;
            }

            return currentlySelectedId;
        }
    }
}