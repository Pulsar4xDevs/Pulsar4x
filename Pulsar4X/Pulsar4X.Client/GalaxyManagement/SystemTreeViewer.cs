using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Extensions;

namespace Pulsar4X.SDL2UI;
public class SystemTreeViewer : PulsarGuiWindow
{
    private SystemTreeViewer()
    {
    }

    internal static SystemTreeViewer GetInstance() {
        SystemTreeViewer thisItem;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(SystemTreeViewer)))
        {
            thisItem = new SystemTreeViewer();
        }
        else
        {
            thisItem = (SystemTreeViewer)_uiState.LoadedWindows[typeof(SystemTreeViewer)];
        }

        return thisItem;
    }

    //displays selected entity info
    internal override void Display()
    {
        if(!IsActive) return;

        if (ImGui.Begin("System Viewer", _flags))
        {
            if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid))
            {
                SystemState starSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
                List<EntityState> stars = starSystemState.EntityStatesWithPosition.Values
                    .Where(e => e.IsStar())
                    .OrderBy(x => x.Position?.AbsolutePosition ?? Orbital.Vector3.Zero)
                    .ToList();

                if(ImGui.BeginTable("DesignStatsTables", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None);
                    ImGui.TableSetupColumn("", ImGuiTableColumnFlags.None);
                    ImGui.TableHeadersRow();

                    foreach (var body in stars)
                    {
                        TreeGen(body.Entity, _uiState.LastClickedEntity?.Entity ?? body.Entity);
                    }

                    ImGui.EndTable();
                }
            }
        }
    }

    void TreeGen(Entity currentBody, Entity selectedBody, int depth = 0)
    {
        SystemState starSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
        var entityStates = starSystemState.EntityStatesWithPosition;

        if (entityStates.ContainsKey(currentBody.Id) && (currentBody.HasDataBlob<StarInfoDB>() || currentBody.HasDataBlob<SystemBodyInfoDB>()))
        {
            if(!currentBody.TryGetDatablob<PositionDB>(out var positionDB))
                return;

            PrintEntity(currentBody, depth);

            var children = positionDB.Children;
            if (children.Count > 0)
            {
                children = children.OrderBy(x => x.GetDataBlob<PositionDB>().AbsolutePosition).ToList();
                foreach (var child in children)
                {
                    TreeGen(child, selectedBody, depth + 1);
                }
            }
        }
    }

    private void PrintEntity(Entity entity, int depth = 0)
    {
        var bodyType = entity.HasDataBlob<SystemBodyInfoDB>() ?
            entity.GetDataBlob<SystemBodyInfoDB>().BodyType.ToDescription() :
            "Star";

        ImGui.TableNextColumn();
        if(depth > 0) ImGui.Indent(16 * depth);
        ImGui.Text(entity.GetName(_uiState.Faction.Id));
        if(depth > 0) ImGui.Unindent(16 * depth);
        ImGui.TableNextColumn();
        ImGui.Text(bodyType);
        ImGui.TableNextColumn();
        ImGui.Text(entity.IsOrHasColony() ? "C" : "");
    }
}
