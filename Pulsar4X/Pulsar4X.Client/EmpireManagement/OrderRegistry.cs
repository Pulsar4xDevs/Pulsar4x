using System;
using System.Collections.Generic;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Orders;
using Pulsar4X.DataStructures;

public static class OrderRegistry
{
    public static Dictionary<string, Func<int, Entity, EntityCommand>> Actions = new Dictionary<string, Func<int, Entity, EntityCommand>>()
    {
        { "Move to Nearest Colony", (factionId, fleet) => MoveToNearestColonyAction.CreateCommand(factionId, fleet) },
        { "Move to Nearest Geo Survey", (factionId, fleet) => MoveToNearestGeoSurveyAction.CreateCommand(factionId, fleet) },
        { "Move to Nearest Anomaly", (factionId, fleet) =>  MoveToNearestAnomalyAction.CreateCommand(factionId, fleet) },
        { "Refuel", (factionId, fleet) => new RefuelAction() },
        { "Resupply", (factionId, fleet) => new ResupplyAction() }
    };

    public static Dictionary<Type, string> ActionDescriptions = new Dictionary<Type, string>()
    {
        { typeof(MoveToNearestColonyAction), "Move to Nearest Colony" },
        { typeof(MoveToNearestGeoSurveyAction), "Move to Nearest Geo Survey" },
        { typeof(MoveToNearestAnomalyAction), "Move to Nearest Anomaly" },
        { typeof(RefuelAction), "Refuel" },
        { typeof(ResupplyAction), "Resupply" },
    };

    public static Dictionary<string, Func<ConditionItem>> Conditions = new Dictionary<string, Func<ConditionItem>>()
    {
        { "Fuel (Fleet Avg)", () => new ConditionItem(new FuelCondition(30f, ComparisonType.LessThan))}
    };

    public static Dictionary<Type, string> ConditionDescriptions = new Dictionary<Type, string>()
    {
        { typeof(FuelCondition), "Fuel (Fleet Avg)" }
    };
}