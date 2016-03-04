using System;

namespace Pulsar4X.ECSLib
{
    public class JumpPointStabilizationAbilityDB : BaseDataBlob
    {
        /// <summary>
        /// Number of days to stabilize a jump point
        /// </summary>
        public int StabilizationTime { get; internal set; }

        public JumpPointStabilizationAbilityDB() { }

        public JumpPointStabilizationAbilityDB(double stabilizationTime) : this((int)stabilizationTime) { }

        public JumpPointStabilizationAbilityDB(int stabilizationTime)
        {
            StabilizationTime = stabilizationTime;
        }

        public override object Clone()
        {
            return new JumpPointStabilizationAbilityDB(StabilizationTime);
        }
    }
}
