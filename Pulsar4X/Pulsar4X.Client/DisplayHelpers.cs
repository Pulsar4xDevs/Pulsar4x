using System;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI
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
            if(!ship.TryGetDatablob<ShipInfoDB>(out var shipInfo))
                return;

            if(!ship.TryGetDatablob<OrderableDB>(out var orderableDB))
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

            DescriptiveTooltip(ship.GetName(factionId), shipInfo.Design.Name, description, meta);
        }

        public static void DescriptiveTooltip(string name, string type, string description, string metaInfo = "", bool hideTypeIfSameAsName = false)
        {
            if(ImGui.IsItemHovered())
            {
                ImGui.SetNextWindowSize(Styles.ToolTipsize);
                ImGui.BeginTooltip();
                ImGui.Text(name);
                if(type.IsNotNullOrEmpty() && (!hideTypeIfSameAsName || (hideTypeIfSameAsName && !type.Equals(name))))
                {
                    var size = ImGui.GetContentRegionAvail();
                    var textSize = ImGui.CalcTextSize(type);
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(size.X - textSize.X);
                    ImGui.PushStyleColor(ImGuiCol.Text, Styles.HighlightColor);
                    ImGui.Text(type);
                    ImGui.PopStyleColor();
                }
                var showDescription = description.IsNotNullOrEmpty();
                var showMetaInfo = metaInfo.IsNotNullOrEmpty();

                if(showDescription || showMetaInfo)
                {
                    ImGui.Separator();
                }

                ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
                if(showDescription)
                {
                    ImGui.TextWrapped(description);
                }
                if(showMetaInfo)
                {
                    ImGui.Text(metaInfo);
                }
                ImGui.PopStyleColor();
                ImGui.EndTooltip();
            }
        }

        public static void Indent()
        {
            ImGui.InvisibleButton("", Styles.Indent);
            ImGui.SameLine();
        }
    }
}