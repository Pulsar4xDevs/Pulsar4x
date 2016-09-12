using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{

    // Move order within a system
    public class BeamAttackOrder : BaseOrder
    {
        // Owner is the ship in question

        // A move order may have either a specific target entity or a location
        
        [JsonProperty]
        public Entity BeamWeapon {get; internal set;}

        [JsonProperty]
        public Entity Target { get; internal set; }

        // 
        public BeamAttackOrder()
        {
            DelayTime = 0;
            Owner = Entity.InvalidEntity;
            Target = Entity.InvalidEntity;
            BeamWeapon = Entity.InvalidEntity;
            OrderType = orderType.BEAMATTACK;
        }

        public BeamAttackOrder(Entity ship, Entity target, Entity beamWeapon) : this()
        {
            DelayTime = 0;
            Owner = ship;
            Target = target;
            BeamWeapon = beamWeapon;

        }

        public override object Clone()
        {
            BeamAttackOrder order = new BeamAttackOrder();
            order.DelayTime = DelayTime;
            order.Owner = Owner;
            order.Target = Target;
            order.BeamWeapon = BeamWeapon;

            return order;
        }

        public override bool isValid()
        {
            if (Owner == null || Owner == Entity.InvalidEntity)
                return false;

            // The ship needs at least one beam weapon
            if (BeamWeapon == null || BeamWeapon == Entity.InvalidEntity)
                return false;

            if (Target == null || Target == Entity.InvalidEntity)
            {
                return false;
            }
            else
                // @todo: jump point, jump survey point
                if (!Target.HasDataBlob<SystemBodyInfoDB>() && !Target.HasDataBlob<ShipInfoDB>())
                    return false;

            // @todo: further conditions
            // @todo: check maximum speed of targeting?

            return true;
        }

        // have the ship actually fire?
        public override bool processOrder()
        {
            return true;
        }
    }
}
