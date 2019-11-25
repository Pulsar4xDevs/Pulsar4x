using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Attached to entities that are surveyed for the discovery of JumpPoints.
    /// </summary>
    /// <remarks>
    /// This is very inefficient implementation of jump points. 
    /// Every system has 30 of these entities.
    /// This clogs EntityManager space too.
    /// Each of these entities individually stores every faction that scans it?
    /// 
    /// PERFORMANCE OPTIMIZE:
    /// If we ever need to optimize memory usage, this may be something to look at.
    /// </remarks>
    public class JPSurveyableDB : BaseDataBlob
    {
        [JsonProperty]
        public int SurveyPointsRequired;
        [JsonProperty]
        public Dictionary<Entity, int> SurveyPointsAccumulated;
        [JsonProperty]
        public Entity JumpPointTo;
        [JsonProperty]
        public Guid SystemToGuid;

        /// <summary>
        /// Default public constructor for Json
        /// </summary>
        public JPSurveyableDB() { }


        public JPSurveyableDB(int pointsRequired, IDictionary<Entity, int> pointsAccumulated): this(pointsRequired, pointsAccumulated, null, Guid.Empty){

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public JPSurveyableDB(int pointsRequired, IDictionary<Entity, int> pointsAccumulated, Entity jumpPointTo, Guid systemToGuid)
        {
            SurveyPointsRequired = pointsRequired;
            SurveyPointsAccumulated = new Dictionary<Entity, int>(pointsAccumulated);
            JumpPointTo = jumpPointTo;
            SystemToGuid = systemToGuid;
        }

        /// <summary>
        /// ICloneable interface implementation.
        /// </summary>
        /// <returns></returns>
        public override object Clone()
        {
            return new JPSurveyableDB(SurveyPointsRequired, SurveyPointsAccumulated, JumpPointTo, SystemToGuid);
        }
    }
}
