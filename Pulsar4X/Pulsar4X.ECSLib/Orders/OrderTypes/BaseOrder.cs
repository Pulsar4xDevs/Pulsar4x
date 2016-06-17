﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace Pulsar4X.ECSLib
{
    public abstract class BaseOrder
    {
        // Orders can be given a delay before being executed
        public long DelayTime { get; internal set; }
        public Entity Owner { get; internal set; }

        abstract public bool isValid();
        abstract public bool processOrder();
        abstract public object Clone();
    }
}
