using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TranslateMoveDB : BaseDataBlob
    {
        
        public Vector4 TargetPosition_AU { get; internal set; }
        [JsonProperty]
        internal Vector4 CurrentVectorMS;
        [JsonProperty]
        internal double MoveRange_KM;
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
        /// use this one to move to a moving target (though currently it doesn't predict, better to predict movement and move to a vector position)
        /// </summary>
        /// <param name="targetPosition">Target position.</param>
        public TranslateMoveDB(PositionDB targetPosition)
        {
            TargetPositionDB = targetPosition;
            TargetEntity = targetPosition.OwningEntity;
            TargetPosition_AU = targetPosition.AbsolutePosition_AU;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Pulsar4X.ECSLib.TranslateMoveDB"/> class.
        /// Use this one to move to a specific postion vector. 
        /// </summary>
        /// <param name="targetPosition_AU">Target position au.</param>
        public TranslateMoveDB(Vector4 targetPosition_AU)
        {
            TargetPosition_AU = targetPosition_AU;

        }

        public TranslateMoveDB(TranslateMoveDB db)
        {
            TargetPositionDB = db.TargetPositionDB;
            CurrentVectorMS = db.CurrentVectorMS;
            MoveRange_KM = db.MoveRange_KM;
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
