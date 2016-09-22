using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// A single ability can provide multiple types of CP's. Some may even overlap. 
    /// For example, you can have a component that provides 5 Installations CP's, and provides 2 Installations | Ships CP's.
    /// Final result will be 7 Installation CP's, and 2 Ship CP's.
    /// </summary>
    public class ConstructionAtbDB : BaseDataBlob
    {
        public ReadOnlyDictionary<ConstructionType, int> ConstructionPoints => new ReadOnlyDictionary<ConstructionType, int>(InternalConstructionPoints);

        public int InstallationConstrustionPoints => GetConstructionPoints(ConstructionType.Installations);
        public int ShipConstructionPoints => GetConstructionPoints(ConstructionType.Ships);
        public int FighterConstructionPoints => GetConstructionPoints(ConstructionType.Fighters);
        public int OrdnanceConstructionPoints => GetConstructionPoints(ConstructionType.Ordnance);

        [JsonProperty]
        internal Dictionary<ConstructionType, int> InternalConstructionPoints { get; set; } = new Dictionary<ConstructionType, int>();

        public ConstructionAtbDB(IDictionary<ConstructionType, double> constructionPoints) 
            : this(constructionPoints.ToDictionary(constructionPoint => constructionPoint.Key, constructionPoint => (int)constructionPoint.Value)) { }
        
        [JsonConstructor]
        public ConstructionAtbDB(IDictionary<ConstructionType, int> constructionPoints = null)
        {
            if (constructionPoints != null)
            {
                InternalConstructionPoints = new Dictionary<ConstructionType, int>(constructionPoints);
            }
        }

        public override object Clone()
        {
            return new ConstructionAtbDB(ConstructionPoints);
        }

        /// <summary>
        /// Adds up all construstion points this ability provides for a given type.
        /// </summary>
        public int GetConstructionPoints(ConstructionType type)
        {
            int totalConstructionPoints = 0;
            foreach (KeyValuePair<ConstructionType, int> keyValuePair in InternalConstructionPoints)
            {
                ConstructionType entryType = keyValuePair.Key;
                int constructionPoints = keyValuePair.Value;

                if ((entryType & type) != 0)
                {
                    totalConstructionPoints += constructionPoints;
                }
            }
            return totalConstructionPoints;
        }
    }
}