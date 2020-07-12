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

    public interface ICargoable
    {
        Guid ID { get; }
        string Name { get; }
        Guid CargoTypeID { get;  }
        /// <summary>
        /// In Kg/M^3
        /// </summary>
        double Density { get; }
    }

    [StaticData(true, IDPropertyName = "ID")]
    public struct IndustryTypeSD
    {
        public string Name;
        public Guid ID;
    }
}


