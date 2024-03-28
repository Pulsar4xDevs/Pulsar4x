using System;
using System.Collections.Generic;
using Pulsar4X.DataStructures;
using Pulsar4X.Engine;

namespace Pulsar4X.Datablobs
{
    public class ColonyBonusesDB : BaseDataBlob
    {
        public new static List<Type> GetDependencies() => new List<Type>() { typeof(ColonyInfoDB) };

        private Dictionary<AbilityType, float> FactionBonus => OwningEntity != Entity.InvalidEntity && OwningEntity.HasDataBlob<FactionAbilitiesDB>() ? OwningEntity.GetDataBlob<FactionAbilitiesDB>().AbilityBonuses : new Dictionary<AbilityType, float>();

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