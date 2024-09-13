using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Datablobs;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Extensions;
using Pulsar4X.Engine.Orders;

namespace Pulsar4X.SDL2UI;
public class SystemTreeViewer : PulsarGuiWindow
{
    private const string SystemViewPreferencesKey = "system-viewer";

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

        if (ImGui.Begin("System Viewer", ref IsActive, _flags))
        {
            if (_uiState.StarSystemStates.ContainsKey(_uiState.SelectedStarSysGuid))
            {
                var systemViewPreferences = SystemViewPreferences.GetInstance();
                int viewIndex = systemViewPreferences.GetViewIndex(SystemViewPreferencesKey);

                ImGui.Text("View Options: ");
                ImGui.SameLine();
                SystemViewPreferences.GetInstance().DisplayCombo(SystemViewPreferencesKey, selectedIndex => {});

                SystemState starSystemState = _uiState.StarSystemStates[_uiState.SelectedStarSysGuid];
                List<EntityState> stars = starSystemState.EntityStatesWithPosition.Values
                    .Where(e => e.IsStar())
                    .OrderBy(x => x.Position?.AbsolutePosition ?? Orbital.Vector3.Zero)
                    .ToList();

                if(ImGui.BeginTable("DesignStatsTables", 9, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Colony", ImGuiTableColumnFlags.None, 0.075f);
                    ImGui.TableSetupColumn("GeoSurvey", ImGuiTableColumnFlags.None, 0.075f);
                    ImGui.TableSetupColumn("Gravity", ImGuiTableColumnFlags.None, 0.075f);
                    ImGui.TableSetupColumn("Temperature", ImGuiTableColumnFlags.None, 0.075f);
                    ImGui.TableSetupColumn("Atm Pressure", ImGuiTableColumnFlags.None, 0.075f);
                    ImGui.TableSetupColumn("Oxygen", ImGuiTableColumnFlags.None, 0.075f);
                    ImGui.TableSetupColumn("Minerals", ImGuiTableColumnFlags.None, 0.075f);
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

            if(SystemViewPreferences.GetInstance().ShouldDisplay(SystemViewPreferencesKey, entityStates[currentBody.Id].BodyType))
                PrintEntity(currentBody, depth);

            var children = positionDB.Children;
            if (children.Count > 0)
            {
                children = new(children.OrderBy(x => x.GetDataBlob<PositionDB>().AbsolutePosition).ToList());
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

        entity.TryGetDatablob<GeoSurveyableDB>(out var geoSurveyableDB);
        bool isSurveyComplete = geoSurveyableDB == null ? false : geoSurveyableDB.IsSurveyComplete(_uiState.Faction.Id);

        ImGui.TableNextColumn();
        if(depth > 0) ImGui.Indent(16 * depth);
        ImGui.Text(entity.GetName(_uiState.Faction.Id));
        if(depth > 0) ImGui.Unindent(16 * depth);
        ImGui.TableNextColumn();
        ImGui.Text(bodyType);
        ImGui.TableNextColumn();

        var result = entity.IsOrHasColony();
        if(result.Item1 && _uiState.SelectedSystemState.EntityStatesColonies.ContainsKey(result.Item2))
        {
            var colony = _uiState.SelectedSystemState.EntityStatesColonies[result.Item2];
            if(colony.Entity.FactionOwnerID == _uiState.Faction.Id && ImGui.SmallButton(colony.Entity.GetOwnersName() + "###" + result.Item2))
            {
                EconomicsWindow.GetInstance().SetActive(true);
                EconomicsWindow.GetInstance().SelectEntity(colony);
            }
        }
        else
        {
            if(isSurveyComplete && entity.HasDataBlob<ColonizeableDB>())
            {
                if(ImGui.SmallButton("Colonize"))
                {
                    var species = _uiState.Faction.GetDataBlob<FactionInfoDB>().Species[0];
                    var command = CreateColonyOrder.CreateCommand(_uiState.Faction, species, entity);
                    _uiState.Game.OrderHandler.HandleOrder(command);
                }
            }
            else
            {
                ImGui.Text("");
            }
        }
        ImGui.TableNextColumn();
        if(geoSurveyableDB != null)
        {
            if(geoSurveyableDB.HasSurveyStarted(_uiState.Faction.Id))
            {
                if(isSurveyComplete)
                {
                    ImGui.Text("Complete");
                }
                else
                {
                    float percent = (1f - (float)geoSurveyableDB.GeoSurveyStatus[_uiState.Faction.Id] / (float)geoSurveyableDB.PointsRequired) * 100;
                    ImGui.Text(percent.ToString("#.##") + "%%");
                }
            }
            else
            {
                ImGui.Text("Surveyable");
            }
        }
        else
        {
            ImGui.Text("");
        }

        if(isSurveyComplete)
        {
            var bodyInfoDb = entity.GetDataBlob<SystemBodyInfoDB>();
            ImGui.TableNextColumn();
            ImGui.Text(Stringify.Velocity(bodyInfoDb.Gravity));
            ImGui.TableNextColumn();
            ImGui.Text(bodyInfoDb.BaseTemperature.ToString("#.#") + " C");

            if(entity.TryGetDatablob<AtmosphereDB>(out var atmosphereDB))
            {
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Number(atmosphereDB.Pressure));
                ImGui.TableNextColumn();
                if(atmosphereDB.Composition.ContainsKey("oxygen"))
                {
                    ImGui.Text(atmosphereDB.Composition["oxygen"] > 0.001 ? atmosphereDB.Composition["oxygen"].ToString("0.0#") : "trace");
                }
                else
                {
                    ImGui.Text("No");
                }
            }
            else
            {
                ImGui.TableNextColumn();
                ImGui.TableNextColumn();
            }

            if(entity.TryGetDatablob<MineralsDB>(out var mineralsDB))
            {
                ImGui.TableNextColumn();
                ImGui.Text("Yes");
            }
            else
            {
                ImGui.TableNextRow();
            }
        }
        else
        {
            ImGui.TableNextRow();
        }
    }
}
