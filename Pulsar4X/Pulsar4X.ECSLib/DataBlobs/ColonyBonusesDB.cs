using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class ColonyBonusesDB : BaseDataBlob
    {
        private Dictionary<AbilityType, float> FactionBonus => OwningEntity.HasDataBlob<FactionAbilitiesDB>() ? OwningEntity.GetDataBlob<FactionAbilitiesDB>().AbilityBonuses : new Dictionary<AbilityType, float>();

        /// <summary>
        /// Returns the Bonus Modifier for a given ability
        /// </summary>
        /// <param name="type">Ability to check for</param>
        /// <returns>1 if no bonus exists or has been applied</returns>
        public float GetBonus(AbilityType type)
        {
            if (!FactionBonus.ContainsKey(type))
            {
                return 1;
            }

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