using System.Linq;
using System.Numerics;
using ImGuiNET;
using Stringify = Pulsar4X.Api.Stringify;

namespace Pulsar4X.Client.Interface;

public class Displays
{
    /// <summary>Snapshot-based gravitational-anomaly display for UI ported to the API galaxy model.</summary>
    public static void GravitationalAnomlay(Pulsar4X.Api.GravSurveyView gravSurvey)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
        ImGui.TextWrapped("Order a fleet equipped with a gravitational surveyor here. A successful survey may reveal a Jump Point to another system.");
        ImGui.PopStyleColor();

        ImGui.Columns(2, "##anomaly-tooltip", false);
        ImGui.SetColumnWidth(0, 140);

        TooltipLabel("Gravity Survey");
        if(gravSurvey.IsSurveyComplete)
        {
            ImGui.TextColored(Styles.GoodColor, "Complete");
        }
        else if(gravSurvey.HasSurveyStarted)
        {
            ImGui.TextColored(Styles.OkColor, "In progress " + gravSurvey.PercentComplete.ToString("0.#") + "%");
        }
        else
        {
            ImGui.TextColored(Styles.BadColor, "Not started");
        }
        ImGui.NextColumn();

        ImGui.Columns(1);
    }

    /// <summary>Snapshot-based ship tooltip: remaining ΔV against full tanks.</summary>
    public static void Ship(Pulsar4X.Api.ThrustView thrust)
    {
        string deltaVStr = Stringify.Velocity(thrust.DeltaVMps) + " Δv";
        float fraction = thrust.MaxDeltaVMps > 0 ? (float)(thrust.DeltaVMps / thrust.MaxDeltaVMps) : 0;
        var size = ImGui.GetContentRegionAvail();
        ImGui.ProgressBar(fraction, new Vector2(size.X, 24), deltaVStr);
    }

    /// <summary>Snapshot-based system-body tooltip.</summary>
    public static void SystemBody(Pulsar4X.Api.EntitySnapshot body, Pulsar4X.Api.IClientSystem system)
    {
        var bodyView = body.GetView<Pulsar4X.Api.BodyView>();
        if (bodyView == null) return;

        int? parentId = body.GetView<Pulsar4X.Api.PositionView>()?.ParentId
            ?? body.GetView<Pulsar4X.Api.OrbitView>()?.ParentId;
        if (parentId is int pid && system.GetEntity(pid)?.GetView<Pulsar4X.Api.NameView>() is { } parentName)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextWrapped("Orbiting " + parentName.Name);
            ImGui.PopStyleColor();
        }

        ImGui.Columns(2, "##sysbody-tooltip", false);
        ImGui.SetColumnWidth(0, 140);

        TooltipLabel("Colony");
        Pulsar4X.Api.EntitySnapshot? colony = null;
        foreach (var candidate in system.Entities)
        {
            if (candidate.GetView<Pulsar4X.Api.ColonyView>()?.PlanetEntityId == body.Id)
            {
                colony = candidate;
                break;
            }
        }
        if (colony != null)
        {
            var ownerColor = colony.Relation == Pulsar4X.Api.OwnerRelation.Owned ? Styles.GoodColor : Styles.OkColor;
            ImGui.TextColored(ownerColor, colony.GetView<Pulsar4X.Api.NameView>()?.Name ?? "Unknown");
        }
        else
        {
            ImGui.TextColored(Styles.NeutralColor, "None");
        }
        ImGui.NextColumn();

        bool isSurveyed = false;
        TooltipLabel("Geo Survey");
        if (body.GetView<Pulsar4X.Api.GeoSurveyView>() is { } geoSurvey)
        {
            if (geoSurvey.IsSurveyComplete)
            {
                isSurveyed = true;
                ImGui.TextColored(Styles.GoodColor, "Complete");
            }
            else if (geoSurvey.HasSurveyStarted)
            {
                ImGui.TextColored(Styles.OkColor, "In progress " + geoSurvey.PercentComplete.ToString("0.#") + "%");
            }
            else
            {
                ImGui.TextColored(Styles.BadColor, "Not started");
            }
        }
        else
        {
            ImGui.TextColored(Styles.NeutralColor, "Not surveyable");
        }
        ImGui.NextColumn();

        if (isSurveyed)
        {
            var atmo = body.GetView<Pulsar4X.Api.AtmosphereView>();

            TooltipLabel("Gravity");
            ImGui.Text(Stringify.Velocity(bodyView.GravityMetresPerSec2));
            ImGui.NextColumn();

            TooltipLabel("Temperature");
            double temp = atmo?.SurfaceTemperatureC ?? bodyView.SurfaceTemperatureC;
            ImGui.Text(temp.ToString("0.#") + " °C");
            ImGui.NextColumn();

            TooltipLabel("Atmosphere");
            if (atmo != null)
                ImGui.Text(Stringify.Quantity(atmo.PressureAtm) + " atm");
            else
                ImGui.TextColored(Styles.NeutralColor, "None");
            ImGui.NextColumn();

            if (atmo != null)
            {
                TooltipLabel("Oxygen");
                var oxygen = atmo.Composition.FirstOrDefault(g => g.Id == "oxygen");
                if (oxygen != null)
                {
                    if (oxygen.PartialPressureAtm > 0.001f)
                        ImGui.TextColored(Styles.GoodColor, oxygen.PartialPressureAtm.ToString("0.0##") + " atm");
                    else
                        ImGui.TextColored(Styles.OkColor, "Trace");
                }
                else
                {
                    ImGui.TextColored(Styles.NeutralColor, "None");
                }
                ImGui.NextColumn();

                TooltipLabel("Hydrosphere");
                if (atmo.Hydrosphere)
                    ImGui.TextColored(Styles.GoodColor, "Yes");
                else
                    ImGui.TextColored(Styles.NeutralColor, "No");
                ImGui.NextColumn();
            }

            TooltipLabel("Minerals");
            if (body.HasView<Pulsar4X.Api.MineralDepositsView>())
                ImGui.TextColored(Styles.GoodColor, "Detected");
            else
                ImGui.TextColored(Styles.NeutralColor, "None");
            ImGui.NextColumn();

            TooltipLabel("Colonizable");
            if (body.HasView<Pulsar4X.Api.ColonizableView>())
            {
                if (bodyView.SupportsPopulations)
                    ImGui.TextColored(Styles.GoodColor, "Yes");
                else
                    ImGui.TextColored(Styles.OkColor, "Requires infrastructure");
            }
            else
            {
                ImGui.TextColored(Styles.NeutralColor, "No");
            }
            ImGui.NextColumn();
        }

        ImGui.Columns(1);
    }

    /// <summary>Snapshot-based star tooltip.</summary>
    public static void Star(Pulsar4X.Api.StarView star)
    {
        ImGui.Columns(2, "##star-tooltip", false);
        ImGui.SetColumnWidth(0, 140);

        TooltipLabel("Spectral Type");
        ImGui.Text(star.SpectralType + star.SpectralSubDivision.ToString() + " " + star.LuminosityClass);
        ImGui.NextColumn();

        TooltipLabel("Class");
        ImGui.Text(star.SpectralClass ?? "");
        ImGui.NextColumn();

        TooltipLabel("Temperature");
        ImGui.Text(star.SurfaceTemperatureC.ToString("#,##0") + " °C");
        ImGui.NextColumn();

        TooltipLabel("Luminosity");
        ImGui.Text(star.Luminosity.ToString("0.###") + " L☉");
        ImGui.NextColumn();

        TooltipLabel("Habitable Zone");
        ImGui.Text(star.MinHabitableRadiusAu.ToString("0.##") + " – " + star.MaxHabitableRadiusAu.ToString("0.##") + " AU");
        ImGui.NextColumn();

        ImGui.Columns(1);
    }

    /// <summary>
    /// Display tooltip for a ship.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="shipInfoDB"></param>
    /// <param name="massVolumeDB"></param>
    /// <param name="positionDB"></param>

    private static void TooltipLabel(string text)
    {
        ImGui.TextColored(Styles.DescriptiveColor, text);
        ImGui.NextColumn();
    }
}
