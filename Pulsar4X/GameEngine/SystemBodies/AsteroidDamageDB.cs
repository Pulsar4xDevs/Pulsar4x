using System;
using Newtonsoft.Json;
using Pulsar4X.DataStructures;

namespace Pulsar4X.Datablobs
{
    public class AsteroidDamageDB : BaseDataBlob
    {
        /// <summary>
        /// Asteroids are damageable and need to store their health value.
        /// </summary>
        [JsonProperty]
        private int _health = 100;

        public int Health
        {
            get { return _health; }
            internal set { _health = value; }
        }

        public PercentValue FractureChance { get; internal set; }



        /// <summary>
        /// Default constructor.
        /// </summary>
        public AsteroidDamageDB()
        {
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="clone"></param>
        public AsteroidDamageDB(AsteroidDamageDB clone)
        {
            _health = clone._health;
        }

        // Datablobs must implement the IClonable interface.
        // Most datablobs simply call their own constructor like so:
        public override object Clone()
        {
            return new AsteroidDamageDB(this);
        }

        public bool Equals(AsteroidDamageDB? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return _health == other._health && FractureChance.Equals(other.FractureChance);
        }
    }
}
