using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;




namespace Pulsar4X.ECSLib
{
    public enum orderType { INVALIDORDER, MOVETO, MOVECARGO, BEAMATTACK, RESEARCH, CONSTRUCTION };
    [Obsolete]
    public abstract class BaseOrder 
    {
        // Orders can be given a delay before being executed
        public long DelayTime { get; internal set; } = 0;
        public Entity Owner { get; internal set; }
        public orderType OrderType { get; internal set; }

        abstract public bool isValid(); 
        abstract public bool processOrder();
        abstract public object Clone();
    }
}
