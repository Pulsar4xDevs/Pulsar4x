namespace Pulsar4X.ECSLib
{
    public enum PlatformRestriction
    {
        None = 0,
        FighterOnly,
        FACOnly,
        FullsizeOnly,
        PDCOnly,
    }

    public enum PointDefenseRestriction
    {
        None = 0,
        FinalFireOnly,
    }

    public class BeamFireControlAbilityDB : BaseDataBlob
    {
        public int Range { get; internal set; }
        public int TrackingSpeed { get; internal set; }

        public int EWHardening { get; internal set; }
        public PlatformRestriction PlatformRestriction { get; internal set; }
        public PointDefenseRestriction PointDefenseRestriction { get; internal set; }
        
        public override object Clone()
        {
            return new BeamFireControlAbilityDB {Range = Range, TrackingSpeed = TrackingSpeed, EWHardening = EWHardening, PlatformRestriction = PlatformRestriction, OwningEntity = OwningEntity};
        }
    }
}
