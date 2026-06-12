using System.Linq;
using System.Numerics;
using ImGuiNET;
using Pulsar4X.Colonies;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.GeoSurveys;
using Pulsar4X.Industry;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;
using Pulsar4X.Names;
using Pulsar4X.Ships;
using Pulsar4X.Storage;
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

    /// <summary>
    /// Display tooltip for a gravitational anomaly.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="jPSurveyableDB"></param>
    public static void GravitationalAnomlay(GlobalUIState state, JPSurveyableDB jPSurveyableDB)
    {
        int factionId = state.Faction?.Id ?? Game.NeutralFactionId;

        ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
        ImGui.TextWrapped("Order a fleet equipped with a gravitational surveyor here. A successful survey may reveal a Jump Point to another system.");
        ImGui.PopStyleColor();

        ImGui.Columns(2, "##anomaly-tooltip", false);
        ImGui.SetColumnWidth(0, 140);

        TooltipLabel("Gravity Survey");
        if(jPSurveyableDB.IsSurveyComplete(factionId))
        {
            ImGui.TextColored(Styles.GoodColor, "Complete");
        }
        else if(jPSurveyableDB.HasSurveyStarted(factionId))
        {
            float percent = (1f - (float)jPSurveyableDB.SurveyPointsRemaining[factionId] / jPSurveyableDB.PointsRequired) * 100f;
            ImGui.TextColored(Styles.OkColor, "In progress " + percent.ToString("0.#") + "%");
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
    public static void Ship(GlobalUIState state, ShipInfoDB shipInfoDB, MassVolumeDB massVolumeDB, PositionDB positionDB, CargoDefinitionsLibrary cargoLibrary)
    {
        if(shipInfoDB.OwningEntity == null)
            return;

        var (fuelType, fuelPercent) = shipInfoDB.OwningEntity.GetFuelInfo(cargoLibrary);
        string fuelStr = "Fuel (" + (fuelPercent * 100) + "%) ";
        if (shipInfoDB.OwningEntity.TryGetDataBlob<NewtonThrustAbilityDB>(out var newtDB))
            fuelStr += Stringify.Velocity(newtDB.DeltaV) + " Δv";
        var size = ImGui.GetContentRegionAvail();

        ImGui.ProgressBar((float)fuelPercent, new Vector2(size.X, 24), fuelStr);
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(fuelType?.Name ?? "Unknown");
        }
    }

    public static void SystemBody(GlobalUIState state, SystemBodyInfoDB systemBodyInfoDB, MassVolumeDB massVolumeDB, PositionDB positionDB)
    {
        var entity = systemBodyInfoDB.OwningEntity;
        if(entity == null) return;

        int factionId = state.Faction?.Id ?? Game.NeutralFactionId;

        if(positionDB.Parent != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Styles.DescriptiveColor);
            ImGui.TextWrapped("Orbiting " + positionDB.Parent.GetDataBlob<NameDB>().GetName(factionId));
            ImGui.PopStyleColor();
        }

        ImGui.Columns(2, "##sysbody-tooltip", false);
        ImGui.SetColumnWidth(0, 140);

        TooltipLabel("Colony");
        var (hasColony, colonyId) = entity.IsOrHasColony();
        if(hasColony && entity.Manager != null && entity.Manager.TryGetEntityById(colonyId, out var colonyEntity))
        {
            var ownerColor = colonyEntity.FactionOwnerID == factionId ? Styles.GoodColor : Styles.OkColor;
            ImGui.TextColored(ownerColor, colonyEntity.GetOwnersName());
        }
        else
        {
            ImGui.TextColored(Styles.NeutralColor, "None");
        }
        ImGui.NextColumn();

        bool isSurveyed = false;
        TooltipLabel("Geo Survey");
        if(entity.TryGetDataBlob<GeoSurveyableDB>(out var geoSurveyableDB))
        {
            if(geoSurveyableDB.IsSurveyComplete(factionId))
            {
                isSurveyed = true;
                ImGui.TextColored(Styles.GoodColor, "Complete");
            }
            else if(geoSurveyableDB.HasSurveyStarted(factionId))
            {
                float percent = (1f - (float)geoSurveyableDB.GeoSurveyStatus[factionId] / geoSurveyableDB.PointsRequired) * 100f;
                ImGui.TextColored(Styles.OkColor, "In progress " + percent.ToString("0.#") + "%");
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

        if(isSurveyed)
        {
            TooltipLabel("Gravity");
            ImGui.Text(Stringify.Velocity(systemBodyInfoDB.Gravity));
            ImGui.NextColumn();

            bool hasAtmo = entity.TryGetDataBlob<AtmosphereDB>(out var atmo);

            TooltipLabel("Temperature");
            float temp = hasAtmo ? atmo.SurfaceTemperature : systemBodyInfoDB.BaseTemperature;
            ImGui.Text(temp.ToString("0.#") + " °C");
            ImGui.NextColumn();

            TooltipLabel("Atmosphere");
            if(hasAtmo)
                ImGui.Text(Stringify.Quantity(atmo.Pressure) + " atm");
            else
                ImGui.TextColored(Styles.NeutralColor, "None");
            ImGui.NextColumn();

            if(hasAtmo)
            {
                TooltipLabel("Oxygen");
                if(atmo.Composition.TryGetValue("oxygen", out var oxygen))
                {
                    if(oxygen > 0.001f)
                        ImGui.TextColored(Styles.GoodColor, oxygen.ToString("0.0##") + " atm");
                    else
                        ImGui.TextColored(Styles.OkColor, "Trace");
                }
                else
                {
                    ImGui.TextColored(Styles.NeutralColor, "None");
                }
                ImGui.NextColumn();

                TooltipLabel("Hydrosphere");
                if(atmo.Hydrosphere)
                    ImGui.TextColored(Styles.GoodColor, "Yes");
                else
                    ImGui.TextColored(Styles.NeutralColor, "No");
                ImGui.NextColumn();
            }

            TooltipLabel("Minerals");
            if(entity.HasDataBlob<MineralsDB>())
                ImGui.TextColored(Styles.GoodColor, "Detected");
            else
                ImGui.TextColored(Styles.NeutralColor, "None");
            ImGui.NextColumn();

            TooltipLabel("Colonizable");
            if(entity.HasDataBlob<ColonizeableDB>())
            {
                if(systemBodyInfoDB.SupportsPopulations)
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

    public static void Star(GlobalUIState state, StarInfoDB starInfoDB)
    {
        ImGui.Columns(2, "##star-tooltip", false);
        ImGui.SetColumnWidth(0, 140);

        TooltipLabel("Spectral Type");
        ImGui.Text(starInfoDB.SpectralType + starInfoDB.SpectralSubDivision.ToString() + " " + starInfoDB.LuminosityClass);
        ImGui.NextColumn();

        TooltipLabel("Class");
        ImGui.Text(starInfoDB.Class ?? "");
        ImGui.NextColumn();

        TooltipLabel("Temperature");
        ImGui.Text(starInfoDB.Temperature.ToString("#,##0") + " °C");
        ImGui.NextColumn();

        TooltipLabel("Luminosity");
        ImGui.Text(starInfoDB.Luminosity.ToString("0.###") + " L☉");
        ImGui.NextColumn();

        TooltipLabel("Habitable Zone");
        ImGui.Text(starInfoDB.MinHabitableRadius_AU.ToString("0.##") + " – " + starInfoDB.MaxHabitableRadius_AU.ToString("0.##") + " AU");
        ImGui.NextColumn();

        ImGui.Columns(1);
    }

    private static void TooltipLabel(string text)
    {
        ImGui.TextColored(Styles.DescriptiveColor, text);
        ImGui.NextColumn();
    }
}