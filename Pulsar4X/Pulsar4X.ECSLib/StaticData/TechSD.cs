using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public struct TechSD
    {
        public string Name;
        public List<TechSD> Reqirements;
        public int cost;
    }
}
