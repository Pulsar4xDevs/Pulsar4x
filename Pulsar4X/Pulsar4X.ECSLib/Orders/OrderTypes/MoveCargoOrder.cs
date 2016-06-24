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

        // A move order may have either a specific target entity or a location

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
        }

        public MoveCargoOrder(Entity owner, Entity origin, Entity target, Guid cargoType, double amount)
        {
            Owner = owner;
            Origin = origin;
            Target = target;
            CargoType = cargoType;
            Amount = amount;
        }

        public override bool isValid()
        {
            throw new NotImplementedException();
        }

        public override bool processOrder()
        {
            throw new NotImplementedException();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

    }
}
