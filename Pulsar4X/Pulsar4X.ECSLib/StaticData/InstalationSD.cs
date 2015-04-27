using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public enum InstalationAbilityType
    {
        ShipMaintanance,
        InstalationConstruction,
        OrdnanceConstruction,
        FighterConstruction,
        FuelRefinary,
        Mine,
        AtmosphericModification,
        Research,
        Comercial, //ie aurora "Finance Center" 
        MassDriver,

    }

    public struct InstalationSD
    {
        public int PopulationReqired;
        public int CargoSize;
        public InstalationAbilityType AbilityType;
        public int AbilityAmount;
    }
}
