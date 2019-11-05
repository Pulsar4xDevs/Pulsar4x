using System;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{


    /// <summary>
    /// This datablob gets added to an entity when that entity is doing non-newtonion translation type movement.
    /// It gets removed from the entity once the entity has finished the translation. 
    /// </summary>
    public class WarpMovingDB : BaseDataBlob
    {
        [JsonProperty]
        public DateTime LastProcessDateTime = new DateTime();

        public Vector3 SavedNewtonionVector { get; internal set; }
        [JsonProperty]
        public Vector3 SavedNewtonionVector_AU
        {
            get => Distance.MToAU(SavedNewtonionVector);
            internal set => SavedNewtonionVector = Distance.AuToMt(value);
        }

        [JsonProperty]
        public Vector3 TranslateEntryAbsolutePoint { get; internal set; }
        public Vector3 TranslateEntryAbsolutePoint_AU
        {
            get => Distance.MToAU(TranslateEntryAbsolutePoint);
            internal set => TranslateEntryAbsolutePoint = Distance.AuToMt(value);
        }
        [JsonProperty]
        public Vector3 TranslateExitPoint { get; internal set; }
        public Vector3 TranslateExitPoint_AU {
            get => Distance.MToAU(TranslateExitPoint);
            internal set => TranslateExitPoint = Distance.AuToMt(value);
        }
        [JsonProperty]
        public Vector3 TranslateRelitiveExit { get; internal set; }
        public Vector3 TranslateRalitiveExit_AU {
            get => Distance.MToAU(TranslateRelitiveExit);
            internal set => TranslateRelitiveExit = Distance.AuToMt(value);
        }
        [JsonProperty]
        public float Heading_Radians { get; internal set; }
        [JsonProperty]
        public DateTime EntryDateTime { get; internal set; }
        [JsonProperty]
        public DateTime PredictedExitTime { get; internal set; }

        [JsonProperty]
        internal Vector3 CurrentNonNewtonionVectorMS { get; set; }

        /// <summary>
        /// m/s
        /// </summary>
        [JsonProperty]
        internal Vector3 ExpendDeltaV { get; set; }
        internal Vector3 ExpendDeltaV_AU {
            get => Distance.MToAU(ExpendDeltaV);
            set => ExpendDeltaV = Distance.AuToMt(value);
        }

        [JsonProperty]
        internal bool IsAtTarget { get; set; }

        [JsonProperty]
        internal Entity TargetEntity;
        [JsonIgnore] //don't store datablobs, we catch this on deserialization. 
        internal PositionDB TargetPositionDB;

        public WarpMovingDB()
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.TranslateMoveDB"/> class.
        /// Use this one to move to a specific postion vector. 
        /// </summary>
        /// <param name="targetPosition_m">Target position in Meters.</param>
        public WarpMovingDB(Vector3 targetPosition_m)
        {
            TranslateExitPoint = targetPosition_m;
            Heading_Radians = (float)Math.Atan2(targetPosition_m.Y, targetPosition_m.X);
        }

        public WarpMovingDB(WarpMovingDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            SavedNewtonionVector = db.SavedNewtonionVector;
            TranslateEntryAbsolutePoint = db.TranslateEntryAbsolutePoint;
            TranslateExitPoint = db.TranslateExitPoint;
            CurrentNonNewtonionVectorMS = db.CurrentNonNewtonionVectorMS;
            ExpendDeltaV = db.ExpendDeltaV;
            IsAtTarget = db.IsAtTarget;
            TargetEntity = db.TargetEntity;

            TargetPositionDB = db.TargetPositionDB;

        }
        // JSON deserialization callback.
        [OnDeserialized]
        private void Deserialized(StreamingContext context)
        {

            if (TargetEntity != null)
            {

                var game = (Game)context.Context;
                game.PostLoad += (sender, args) =>
                {
                    TargetPositionDB = TargetEntity.GetDataBlob<PositionDB>();
                };
            }
        }

        public override object Clone()
        {
            return new WarpMovingDB(this);
        }
    }
}
