using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class FailureRateMitigationAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public int FailureRateMitigation { get; internal set; }

        public FailureRateMitigationAtbDB(double failureRateMitigation) : this((int)failureRateMitigation) { }

        [JsonConstructor]
        public FailureRateMitigationAtbDB(int failureRateMitigation = 0)
        {
            FailureRateMitigation = failureRateMitigation;
        }

        public override object Clone()
        {
            return new FailureRateMitigationAtbDB(FailureRateMitigation);
        }
    }
}
