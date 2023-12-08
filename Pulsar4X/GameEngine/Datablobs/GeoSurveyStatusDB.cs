using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Datablobs;

/// <summary>
/// This class tracks the geo survey status of a system body by faction ID
/// </summary>
public class GeoSurveyStatusDB : BaseDataBlob
{
    public new static List<Type> GetDependencies() => new List<Type>() { typeof(SystemBodyInfoDB) };

    /// <summary>
    /// Key represents a faction ID
    /// Value is how many points remain for the survey to be complete by that faction
    /// </summary>
    [JsonProperty]
    public SafeDictionary<int, int> PointsRemainingByFaction { get; set; } = new ();
}