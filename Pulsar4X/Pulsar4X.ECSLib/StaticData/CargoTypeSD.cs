using System;

namespace Pulsar4X.ECSLib
{
    [StaticData(true, IDPropertyName = "ID")]
    public struct CargoTypeSD
    {
        public string Name;
        public string Description;
        public Guid ID;
    }
}


