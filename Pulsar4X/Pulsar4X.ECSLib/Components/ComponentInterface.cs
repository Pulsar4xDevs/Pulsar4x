using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{

    public class Component
    {
        public Entity DesignEntity;
        List<IComponentDesignAttribute> Attributes;

        int NumberOfThisDesignOnShip { get { return InstancesOfDesign.Count; } }
        List<Entity> InstancesOfDesign;
                


    }
}
