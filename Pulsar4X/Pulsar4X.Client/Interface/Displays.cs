using System.Numerics;
using ImGuiNET;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;
using Pulsar4X.Galaxy;
using Pulsar4X.JumpPoints;
using Pulsar4X.Movement;
using Pulsar4X.SDL2UI;
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
        ImGui.PushStyleColor(ImGuiCol.Text, Styles.OkColor);
        ImGui.Text("Gravitational anomaly!");
        ImGui.PopStyleColor();
        ImGui.NewLine();
        ImGui.TextWrapped("Order a fleet equipped with a gravitational surveyor to survey this location. A successful survey may reveal a Jump Point to another system.");
        ImGui.NewLine();

        var factionID = state.Faction.Id;
        var remainingPoints = jPSurveyableDB.PointsRequired;
        if( jPSurveyableDB.SurveyPointsRemaining.ContainsKey(factionID))
            remainingPoints = jPSurveyableDB.SurveyPointsRemaining[factionID];

        ImGui.TextWrapped("Survey Points Required: " + remainingPoints + "/" + jPSurveyableDB.PointsRequired);
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
        if (shipInfoDB.OwningEntity.TryGetDatablob<NewtonThrustAbilityDB>(out var newtDB))
            fuelStr += Stringify.Velocity(newtDB.DeltaV) + " Δv";
        var size = ImGui.GetContentRegionAvail();

        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Styles.SelectedColor);
        ImGui.ProgressBar((float)fuelPercent, new Vector2(size.X, 24), fuelStr);
        ImGui.PopStyleColor();
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(fuelType?.Name ?? "Unknown");
        }
    }
}