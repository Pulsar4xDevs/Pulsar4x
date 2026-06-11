using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using Pulsar4X.Api;
using Pulsar4X.Client.Interface.Widgets;
// Engine using: Stringify formatting helpers only.
using Pulsar4X.Engine;

namespace Pulsar4X.Client;
public class SystemWindow : PulsarGuiWindow
{
    private const string SystemViewPreferencesKey = "system-viewer";

    internal static SystemWindow GetInstance() {
        SystemWindow thisItem;
        if (!_uiState.LoadedWindows.ContainsKey(typeof(SystemWindow)))
        {
            thisItem = new SystemWindow();
        }
        else
        {
            thisItem = (SystemWindow)_uiState.LoadedWindows[typeof(SystemWindow)];
        }

        return thisItem;
    }

    //displays selected entity info
    internal override void Display()
    {
        if(!IsActive) return;

        if (Window.Begin("System Viewer", ref IsActive, _flags))
        {
            var system = _uiState.GameClient?.Galaxy.GetSystem(_uiState.SelectedStarSystemId);
            if (system != null)
            {
                ImGui.Text("View Options: ");
                ImGui.SameLine();
                SystemViewPreferences.GetInstance().DisplayCombo(SystemViewPreferencesKey, selectedIndex => {});

                // The celestial bodies, their orbital hierarchy, and the faction's colonies keyed by
                // the body they sit on — rebuilt each frame from the (faction-filtered) snapshot.
                var bodies = system.Entities
                    .Where(e => e.HasView<StarView>() || e.HasView<BodyView>())
                    .ToDictionary(e => e.Id);

                var children = new Dictionary<int, List<EntitySnapshot>>();
                foreach (var body in bodies.Values)
                {
                    if (ParentIdOf(body) is { } parentId && parentId != body.Id && bodies.ContainsKey(parentId))
                    {
                        if (!children.TryGetValue(parentId, out var list))
                        {
                            list = new List<EntitySnapshot>();
                            children[parentId] = list;
                        }
                        list.Add(body);
                    }
                }

                var coloniesByBody = system.Entities
                    .Where(e => e.Kind == BodyKind.Colony && e.Relation == OwnerRelation.Owned)
                    .Select(e => (Colony: e, PlanetId: e.GetView<ColonyView>()?.PlanetEntityId))
                    .Where(c => c.PlanetId != null)
                    .ToDictionary(c => c.PlanetId!.Value, c => c.Colony);

                var stars = bodies.Values
                    .Where(e => e.HasView<StarView>())
                    .OrderBy(DistanceFromRoot)
                    .ToList();

                if(ImGui.BeginTable("DesignStatsTables", 9, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 0.15f);
                    ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.None, 0.15f);
                    ImGui.TableSetupColumn("Colony", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("GeoSurvey", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Gravity", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Temperature", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Atm Pressure", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Oxygen", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableSetupColumn("Minerals", ImGuiTableColumnFlags.None, 0.1f);
                    ImGui.TableHeadersRow();

                    foreach (var star in stars)
                    {
                        TreeGen(star, children, coloniesByBody);
                    }

                    ImGui.EndTable();
                }
            }
        }
        Window.End();
    }

    private static int? ParentIdOf(EntitySnapshot body)
        => body.GetView<OrbitView>()?.ParentId ?? body.GetView<PositionView>()?.ParentId;

    private static double DistanceFromRoot(EntitySnapshot body)
    {
        var position = body.GetView<PositionView>()?.AbsolutePosition ?? new Vec3(0, 0, 0);
        return position.X * position.X + position.Y * position.Y + position.Z * position.Z;
    }

    // Within a level, order inner -> outer by orbital distance, falling back to name.
    private static IEnumerable<EntitySnapshot> SortBodies(IEnumerable<EntitySnapshot> bodies)
        => bodies.OrderBy(b => b.GetView<OrbitView>()?.SemiMajorAxisKm ?? double.MaxValue)
                 .ThenBy(b => b.GetView<NameView>()?.Name ?? "");

    void TreeGen(EntitySnapshot currentBody, Dictionary<int, List<EntitySnapshot>> children,
        Dictionary<int, EntitySnapshot> coloniesByBody, int depth = 0)
    {
        if(SystemViewPreferences.GetInstance().ShouldDisplay(SystemViewPreferencesKey, UserOrbitSettings.FromBodyKind(currentBody.Kind)))
            PrintEntity(currentBody, coloniesByBody, depth);

        if (children.TryGetValue(currentBody.Id, out var childList))
        {
            foreach (var child in SortBodies(childList))
            {
                TreeGen(child, children, coloniesByBody, depth + 1);
            }
        }
    }

    private void PrintEntity(EntitySnapshot entity, Dictionary<int, EntitySnapshot> coloniesByBody, int depth = 0)
    {
        var bodyType = entity.HasView<StarView>() ? "Star" : entity.GetView<BodyView>()?.BodyType ?? "";

        var geoSurvey = entity.GetView<GeoSurveyView>();
        bool isSurveyComplete = geoSurvey?.IsSurveyComplete ?? false;

        ImGui.TableNextColumn();
        if(depth > 0) ImGui.Indent(16 * depth);
        ImGui.Text(entity.GetView<NameView>()?.Name ?? "Unknown");
        if(depth > 0) ImGui.Unindent(16 * depth);
        ImGui.TableNextColumn();
        ImGui.Text(bodyType);
        ImGui.TableNextColumn();

        if(coloniesByBody.TryGetValue(entity.Id, out var colony))
        {
            var colonyName = colony.GetView<NameView>()?.Name ?? "Colony";
            if(ImGui.SmallButton(colonyName + "###" + colony.Id))
            {
                ColonyManagementWindow.GetInstance().SetActive(true);
                ColonyManagementWindow.GetInstance().SelectColony(colony.Id, _uiState.SelectedStarSystemId);
            }
        }
        else
        {
            if(isSurveyComplete && entity.HasView<ColonizableView>())
            {
                if(ImGui.SmallButton("Colonize") && _uiState.GameClient != null)
                {
                    _uiState.GameClient.SubmitCommandAsync(
                        new CreateColonyCommand(_uiState.GameClient.Session.FactionId, entity.Id));
                }
            }
            else
            {
                ImGui.Text("");
            }
        }
        ImGui.TableNextColumn();
        if(geoSurvey != null)
        {
            if(geoSurvey.HasSurveyStarted)
            {
                if(isSurveyComplete)
                {
                    ImGui.Text("Complete");
                }
                else
                {
                    ImGui.Text(geoSurvey.PercentComplete.ToString("#.##") + "%%");
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
            var body = entity.GetView<BodyView>();
            ImGui.TableNextColumn();
            ImGui.Text(Stringify.Velocity(body?.GravityMetresPerSec2 ?? 0));
            ImGui.TableNextColumn();
            ImGui.Text((body?.SurfaceTemperatureC ?? 0).ToString("#.#") + " C");

            if(entity.GetView<AtmosphereView>() is { } atmosphere)
            {
                ImGui.TableNextColumn();
                ImGui.Text(Stringify.Quantity(atmosphere.PressureAtm));
                ImGui.TableNextColumn();
                var oxygen = atmosphere.Composition.FirstOrDefault(g => g.Id == "oxygen");
                if(oxygen != null)
                {
                    ImGui.Text(oxygen.PartialPressureAtm > 0.001 ? oxygen.PartialPressureAtm.ToString("0.0#") : "trace");
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

            if(entity.HasView<MineralDepositsView>())
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
