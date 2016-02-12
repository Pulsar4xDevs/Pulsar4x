using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public abstract partial class Condition
    {
        public enum ConditionType
        {
            OwnFuelPercet
        }

        public ConditionType Type;
        public List<float> Floats = new List<float>();
        public List<StarSystem> Systems = new List<StarSystem>();
        public List<ProtoEntity> Entities = new List<ProtoEntity>();

        public abstract bool IsMet();
    }
}
