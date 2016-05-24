using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    abstract class PlayerOrder
    {
        //@todo: flesh out
        Entity _owningEntity;

        public PlayerOrder()
        {

        }

        public PlayerOrder(Entity owner)
        {
            _owningEntity = owner;
        }
    }
}
