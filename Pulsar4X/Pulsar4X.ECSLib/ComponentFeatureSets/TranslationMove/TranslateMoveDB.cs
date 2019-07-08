using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{


    /// <summary>
    /// This datablob gets added to an entity when that entity is doing non-newtonion translation type movement.
    /// It gets removed from the entity once the entity has finished the translation. 
    /// </summary>
    public class TranslateMoveDB : BaseDataBlob
    {
        [JsonProperty]
        public DateTime LastProcessDateTime = new DateTime();

        [JsonProperty]
        public Vector3 SavedNewtonionVector_AU { get; internal set; }

        [JsonProperty]
        public Vector3 TranslateEntryPoint_AU { get; internal set; }
        [JsonProperty]
        public Vector3 TranslateExitPoint_AU { get; internal set; }
        [JsonProperty]
        public Vector3 TranslateRalitiveExit_AU { get; internal set; }
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

        [JsonProperty]
        internal bool IsAtTarget { get; set; }

        [JsonProperty]
        internal Entity TargetEntity;
        [JsonIgnore] //don't store datablobs, we catch this on deserialization. 
        internal PositionDB TargetPositionDB;

        public TranslateMoveDB()
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.TranslateMoveDB"/> class.
        /// Use this one to move to a specific postion vector. 
        /// </summary>
        /// <param name="targetPosition_AU">Target position au.</param>
        public TranslateMoveDB(Vector3 targetPosition_AU)
        {
            TranslateExitPoint_AU = targetPosition_AU;
            Heading_Radians = (float)Math.Atan2(targetPosition_AU.Y, targetPosition_AU.X);
        }

        public TranslateMoveDB(TranslateMoveDB db)
        {
            LastProcessDateTime = db.LastProcessDateTime;
            SavedNewtonionVector_AU = db.SavedNewtonionVector_AU;
            TranslateEntryPoint_AU = db.TranslateEntryPoint_AU;
            TranslateExitPoint_AU = db.TranslateExitPoint_AU;
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
            return new TranslateMoveDB(this);
        }
    }
}
