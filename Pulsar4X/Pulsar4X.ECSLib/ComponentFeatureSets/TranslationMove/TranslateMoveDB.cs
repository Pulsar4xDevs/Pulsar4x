using System;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class TranslateMoveDB : BaseDataBlob
    {
        internal Vector4 TargetPosition { get { return TargetPositionDB.AbsolutePosition; } }
        [JsonProperty]
        internal Vector4 CurrentVector;
        [JsonProperty]
        internal double MoveRangeInKM;
        [JsonProperty]
        internal bool IsAtTarget { get; set; }
        [JsonProperty]
        internal PositionDB TargetPositionDB;

        public TranslateMoveDB()
        {
        }

        public TranslateMoveDB(PositionDB targetPosition)
        {
            TargetPositionDB = targetPosition;
        }

        public TranslateMoveDB(TranslateMoveDB db)
        {
            TargetPositionDB = db.TargetPositionDB;
            CurrentVector = db.CurrentVector;
            MoveRangeInKM = db.MoveRangeInKM;
            IsAtTarget = db.IsAtTarget;
        }

        public override object Clone()
        {
            return new TranslateMoveDB(this);
        }
    }
}
