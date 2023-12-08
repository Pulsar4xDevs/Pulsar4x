using Pulsar4X.Datablobs;

namespace Pulsar4X.Extensions;

public static class GeoSurveyStatusDBExtensions
{
    public static bool IsSurveyComplete(this GeoSurveyStatusDB geoSurveyStatusDB, int factionId)
    {
        return geoSurveyStatusDB.PointsRemainingByFaction.ContainsKey(factionId) && geoSurveyStatusDB.PointsRemainingByFaction[factionId] == 0;
    }
}