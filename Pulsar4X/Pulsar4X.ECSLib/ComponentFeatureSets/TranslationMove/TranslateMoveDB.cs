using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

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
        public Vector4 SavedNewtonionVector_AU { get; internal set; }

        [JsonProperty]
        public Vector4 TranslateEntryPoint_AU { get; internal set; }
        [JsonProperty]
        public Vector4 TranslationExitPoint_AU { get; internal set; }
        [JsonProperty]
        public DateTime EntryDateTime { get; internal set; }
        [JsonProperty]
        public DateTime PredictedExitTime { get; internal set; }

        [JsonProperty]
        internal Vector4 CurrentNonNewtonionVectorMS;

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
        public TranslateMoveDB(Vector4 targetPosition_AU)
        {
            TranslationExitPoint_AU = targetPosition_AU;

        }

        public TranslateMoveDB(TranslateMoveDB db)
        {
            TargetPositionDB = db.TargetPositionDB;
            CurrentNonNewtonionVectorMS = db.CurrentNonNewtonionVectorMS;
            IsAtTarget = db.IsAtTarget;
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
