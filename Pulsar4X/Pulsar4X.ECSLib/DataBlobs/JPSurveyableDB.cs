using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Attached to entities that are surveyed for the discovery of JumpPoints.
    /// </summary>
    class JPSurveyableDB : BaseDataBlob
    {
        [JsonProperty]
        public int SurveyPointsRequired;
        [JsonProperty]
        public JDictionary<Entity, int> SurveyPointsAccumulated;

        /// <summary>
        /// Default public constructor for Json
        /// </summary>
        public JPSurveyableDB()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public JPSurveyableDB(int pointsRequired, JDictionary<Entity, int> pointsAccumulated)
        {
            SurveyPointsRequired = pointsRequired;
            SurveyPointsAccumulated = new JDictionary<Entity, int>(pointsAccumulated);
        }

        /// <summary>
        /// ICloneable interface implementation.
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new JPSurveyableDB(SurveyPointsRequired, SurveyPointsAccumulated);
        }
    }
}
