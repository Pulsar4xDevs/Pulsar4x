using System;

namespace Pulsar4X.ECSLib
{
    public class TranslateMoveDB : BaseDataBlob
    {
        internal Vector4 TargetPosition { get { return _targetPosition.AbsolutePosition; } }
        internal double MoveRangeInKM;
        internal bool IsAtTarget { get; set; }
        private PositionDB _targetPosition;

        public TranslateMoveDB()
        {
        }

        public TranslateMoveDB(PositionDB targetPosition)
        {
            _targetPosition = targetPosition;
        }

        public TranslateMoveDB(TranslateMoveDB db)
        {
            _targetPosition = db._targetPosition;
            MoveRangeInKM = db.MoveRangeInKM;
            IsAtTarget = db.IsAtTarget;
        }

        public override object Clone()
        {
            return new TranslateMoveDB(this);
        }
    }
}
