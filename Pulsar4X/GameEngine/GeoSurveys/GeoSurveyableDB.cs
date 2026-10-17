using Newtonsoft.Json;
using Pulsar4X.Datablobs;
using Pulsar4X.DataStructures;

namespace Pulsar4X.GeoSurveys;

public class GeoSurveyableDB : BaseDataBlob
{
    public uint PointsRequired { get; set; }

    /// <summary>
    /// Stores the status of the geosurveys that have been conducted by each faction
    /// Key: faction id
    /// Value: the points remaining to complete the survey
    /// </summary>
    public SafeDictionary<int, uint> GeoSurveyStatus { get; set; } = new ();
}