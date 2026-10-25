using ImGuiNET;
using Pulsar4X.Api;

namespace Pulsar4X.Client
{
    /// <summary>
    /// The Log tab. Oldest line at the top. Opening the tab scrolls to the latest line.
    /// Nothing here edits the book.
    /// </summary>
    static class ShipLogDisplay
    {
        public static void Draw(EntitySnapshot? entity, ref int shownEntityId, ref int shownCount)
        {
            if (!ImGui.BeginChild("ShipLog"))
            {
                ImGui.EndChild();
                return;
            }

            var lines = entity?.GetView<ShipLogView>()?.Lines;
            if (lines == null || lines.Count == 0)
            {
                ImGui.Text("No entries.");
            }
            else
            {
                foreach (var line in lines)
                {
                    bool indent = line.Kind != ShipLogKind.Own;
                    if (indent)
                        ImGui.Indent();
                    ImGui.Text(line.At.ToString("yyyy-MM-dd HH:mm") + "  " + line.Text);
                    if (indent)
                        ImGui.Unindent();
                }

                if (entity!.Id != shownEntityId || lines.Count != shownCount)
                {
                    ImGui.SetScrollHereY(1f);
                    shownEntityId = entity.Id;
                    shownCount = lines.Count;
                }
            }

            ImGui.EndChild();
        }
    }
}
