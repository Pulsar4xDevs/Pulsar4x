using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class BeamFireControlAtbDB : BaseDataBlob, IComponentDesignAttribute
    {
        /// <summary>
        /// Max range of this Beam Fire Control
        /// </summary>
        [JsonProperty]
        public int Range { get; internal set; }

        /// <summary>
        /// Tracking Speed of this Beam Fire Control
        /// </summary>
        [JsonProperty]
        public int TrackingSpeed { get; internal set; }
        
        /// <summary>
        /// Determines if this Beam Fire Control is only capable of FinalDefensiveFire (Like CIWS)
        /// </summary>
        [JsonProperty]
        public bool FinalFireOnly { get; internal set; }

        public BeamFireControlAtbDB(double range, double trackingSpeed) : this((int)range, (int)trackingSpeed) { }

        [JsonConstructor]
        public BeamFireControlAtbDB(int range = 0, int trackingSpeed = 0, bool finalFireOnly = false)
        {
            Range = range;
            TrackingSpeed = trackingSpeed;
            FinalFireOnly = finalFireOnly;
        }

        public override object Clone()
        {
            return new BeamFireControlAtbDB(Range, TrackingSpeed, FinalFireOnly);
        }


        public void OnComponentInstallation(Entity parentEntity, ComponentInstance componentInstance)
        {
            if (!parentEntity.HasDataBlob<FireControlAbilityDB>())
                parentEntity.SetDataBlob<FireControlAbilityDB>(new FireControlAbilityDB());
            var ability = parentEntity.GetDataBlob<FireControlAbilityDB>();
            if (!componentInstance.HasAblity<FireControlAbilityState>())
                componentInstance.SetAbilityState<FireControlAbilityState>(new FireControlAbilityState(componentInstance));
            ability.FireControlInstances.Add(componentInstance);
        }
    }
}
