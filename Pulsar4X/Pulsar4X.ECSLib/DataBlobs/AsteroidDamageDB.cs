using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class AsteroidDamageDB : BaseDataBlob
    {
        /// <summary>
        /// I need these? cargo culting ahead.
        /// </summary>
        private int _importantNumber;
        private ICloneable _cloneableObj;

        /// <summary>
        /// Asteroids are damageable and need to store their health value.
        /// </summary>
        [JsonProperty]
        private Int32 _health;

        [PublicAPI]
        public int Health
        {
            get { return _health; }
            internal set { _health = value; }
        }


        /// <summary>
        /// Default constructor.
        /// </summary>
        public AsteroidDamageDB()
        {
            _health = 100;
        }

        /// <summary>
        /// Deep copy constructor
        /// </summary>
        /// <param name="clone"></param>
        public AsteroidDamageDB(AsteroidDamageDB clone)
        {
            _importantNumber = clone._importantNumber;
            _cloneableObj = (ICloneable)_cloneableObj.Clone();

            _health = 100;
        }

        // Datablobs must implement the IClonable interface.
        // Most datablobs simply call their own constructor like so:
        public override object Clone()
        {
            return new AsteroidDamageDB(this);
        }
    }
}
