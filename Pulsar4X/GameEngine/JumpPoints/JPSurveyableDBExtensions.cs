using Pulsar4X.Datablobs;
using Pulsar4X.Engine;

namespace Pulsar4X.Extensions;

public static class JPSurveyableDBExtensions
{
    public static bool IsSurveyComplete(this JPSurveyableDB geoSurveyableDB, int factionId)
    {
        return geoSurveyableDB.SurveyPointsRemaining.ContainsKey(factionId)
            && geoSurveyableDB.SurveyPointsRemaining[factionId] == 0;
    }

    public static bool HasSurveyStarted(this JPSurveyableDB geoSurveyableDB, int factionId)
    {
        return geoSurveyableDB.SurveyPointsRemaining.ContainsKey(factionId);
    }
}