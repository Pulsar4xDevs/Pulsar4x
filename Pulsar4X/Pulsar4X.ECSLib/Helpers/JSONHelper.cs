using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// The following is a special dictionay. It should be used whenever you have a dictionary 
    /// that will be serilized/deserilized by json and the key is a custom type.
    /// This is because by default JSON.net will save the key as a string only, this
    /// Dictionay forces it to save the full type.
    /// </summary>
    [JsonArrayAttribute]
    public class JDictionary<K, V> : Dictionary<K, V>
    { }
}
