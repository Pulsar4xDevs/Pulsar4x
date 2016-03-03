using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class BeamFireControlAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public int Range { get; internal set; }
        [JsonProperty]
        public int TrackingSpeed { get; internal set; }
        
        [JsonProperty]
        public bool FinalFireOnly { get; internal set; }

        public BeamFireControlAbilityDB() { }

        public BeamFireControlAbilityDB(double range, double trackingSpeed, bool finalFireOnly) : this((int)range, (int)trackingSpeed, finalFireOnly) { }

        public BeamFireControlAbilityDB(int range, int trackingSpeed, bool finalFireOnly)
        {
            Range = range;
            TrackingSpeed = trackingSpeed;
            FinalFireOnly = finalFireOnly;
        }

        public override object Clone()
        {
            return new BeamFireControlAbilityDB(Range, TrackingSpeed, FinalFireOnly);
        }
    }
}
