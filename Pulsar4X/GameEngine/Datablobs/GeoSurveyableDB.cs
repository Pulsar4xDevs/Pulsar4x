using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Datablobs;

public class GeoSurveyableDB : BaseDataBlob
{
    [JsonProperty]
    public uint PointsRequired { get; set; }

    /// <summary>
    /// Stores the status of the geosurveys that have been conducted by each faction
    /// Key: faction id
    /// Value: the points remaining to complete the survey
    /// </summary>
    [JsonProperty]
    public SafeDictionary<int, uint> GeoSurveyStatus { get; set; } = new ();
}