using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MoveCargoOrder : BaseOrder
    {
        // Owner is the ship in question

        // Order indicates where cargo is being moved from (origin) and to (target).  
        // Either origin or target can be the owner

        [JsonProperty]
        public Entity Origin { get; internal set; }

        [JsonProperty]
        public Entity Target { get; internal set; }

        [JsonProperty]
        public Guid CargoType { get; internal set; }

        // Amount of cargo to move.  0 mean move as much as possible
        [JsonProperty]
        public double Amount { get; internal set; }

        public MoveCargoOrder()
        {
            Owner = Entity.InvalidEntity;
            Origin = Entity.InvalidEntity;
            Target = Entity.InvalidEntity;
            CargoType = Entity.InvalidEntity.Guid;
            Amount = 0;
            OrderType = orderType.MOVECARGO;
        }

        public MoveCargoOrder(Entity owner, Entity origin, Entity target, Guid cargoType, double amount) : this()
        {
            Owner = owner;
            Origin = origin;
            Target = target;
            CargoType = cargoType;
            Amount = amount;
        }

        public override bool isValid()
        {
            if (Owner == Entity.InvalidEntity)
                return false;
            if (Target == Entity.InvalidEntity)
                return false;
            if (Origin == Entity.InvalidEntity)
                return false;

            if ((Owner != Target) && (Owner != Origin))  // Owner must be either target or origin
                return false;

            // @todo: more stringent checks?

            return true;

            //throw new NotImplementedException();
        }

        public override bool processOrder()
        {
            throw new NotImplementedException();
        }

        public override object Clone()
        {
            MoveCargoOrder order = new MoveCargoOrder(Owner, Origin, Target, CargoType, Amount);
            return order;

            //throw new NotImplementedException();
        }

    }
}
