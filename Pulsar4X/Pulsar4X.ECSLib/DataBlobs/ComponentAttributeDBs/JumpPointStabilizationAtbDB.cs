using System;

namespace Pulsar4X.ECSLib
{
    public class JumpPointStabilizationAtbDB : BaseDataBlob
    {
        /// <summary>
        /// Number of days to stabilize a jump point
        /// </summary>
        public int StabilizationTime { get; internal set; }

        public JumpPointStabilizationAtbDB() { }

        public JumpPointStabilizationAtbDB(double stabilizationTime) : this((int)stabilizationTime) { }

        public JumpPointStabilizationAtbDB(int stabilizationTime)
        {
            StabilizationTime = stabilizationTime;
        }

        public override object Clone()
        {
            return new JumpPointStabilizationAtbDB(StabilizationTime);
        }
    }
}
