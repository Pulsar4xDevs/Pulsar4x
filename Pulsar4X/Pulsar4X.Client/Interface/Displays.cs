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

namespace Pulsar4X.Client.Interface;

public class Displays
{
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