using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// The following is a special dictionay. It should be used whenever you have a dictionary 
    /// that will be serilized/deserilized by json and the key is a custom type.
    /// This is because by default JSON.net will save the key as a string only, this
    /// Dictionay forces it to save the full type.
    /// </summary>
    [JsonArrayAttribute]
    public class JDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new instance of Pulsar4X.ECSLib.JDicitonary<TKey, TValue> class that is empty, has the default initial capacity, and uses the default equality comparer for the key type.
        /// </summary>
        public JDictionary()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Pulsar4X.ESCLib.JDictionary<TKey, TValue> class that contains elements copied from the specified System.Collections.Generic.IDictionary<TKey, TValue> and uses the default equality comparer for the key type.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <remarks>Acts as a deep copy constructor.</remarks>
        public JDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
        {
        }
    }

    /// <summary>
    /// This class can be used by a Json.net serializer to get around the problem of it ignoreing ISerializable in favor of IEnumarble.
    /// </summary>
    public class ForceUseISerializable : DefaultContractResolver
    {
        protected override JsonContract CreateContract(Type objectType)
        {
            if (typeof(ISerializable).IsAssignableFrom(objectType))
            {
                return CreateISerializableContract(objectType);
            }

            return base.CreateContract(objectType);
        }
    }
}
