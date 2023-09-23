using Pulsar4X.Blueprints;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Extensions
{
    public static class SystemGenSettingsExtensions
    {
        public static WeightedList<BodyType> GetBandBodyTypeWeight(this SystemGenSettingsBlueprint blueprint, SystemBand systemBand)
        {
            switch (systemBand)
            {
                case SystemBand.InnerBand:
                    return blueprint.InnerBandTypeWeights;
                case SystemBand.HabitableBand:
                    return blueprint.HabitableBandTypeWeights;
                case SystemBand.OuterBand:
                default:
                    return blueprint.OuterBandTypeWeights;
            }
        }
    }
}