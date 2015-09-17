using System.Collections.Generic;
using System.Linq;

namespace Pulsar4X.ECSLib
{
    public class ColonyBonusesDB : BaseDataBlob
    {
       
        private JDictionary<AbilityType, float> FactionBonus { get { return this.OwningEntity.GetDataBlob<FactionAbilitiesDB>().AbilityBonuses; }}

        public float GetBonus(AbilityType type)
        {
            return FactionBonus[type]; //* SectorBonus[type] * SystemBonus[type] * PlanetBonus[type] * //RaceBonus[race][type]  ??
        }

        public ColonyBonusesDB()
        {
            
        }

        public ColonyBonusesDB(ColonyBonusesDB db)
        {

        }

        public override object Clone()
        {
            return new ColonyBonusesDB(this);
        }
    }
}