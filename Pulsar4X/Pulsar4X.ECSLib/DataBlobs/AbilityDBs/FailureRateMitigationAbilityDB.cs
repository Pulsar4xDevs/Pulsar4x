using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FailureRateMitigationAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int FailureRateMitigation { get; internal set; }

        public FailureRateMitigationAbilityDB(double failureRateMitigation) : this((int)failureRateMitigation) { }

        [JsonConstructor]
        public FailureRateMitigationAbilityDB(int failureRateMitigation = 0)
        {
            FailureRateMitigation = failureRateMitigation;
        }

        public override object Clone()
        {
            return new FailureRateMitigationAbilityDB(FailureRateMitigation);
        }
    }
}
