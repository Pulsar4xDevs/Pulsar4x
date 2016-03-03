using Newtonsoft.Json;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class NameDB : BaseDataBlob
    {

        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        [JsonProperty]
        private readonly Dictionary<Entity, string> _names = new Dictionary<Entity, string>();

        [PublicAPI]
        public string DefaultName => _names[Entity.InvalidEntity];

        public NameDB() { }

        public NameDB(string defaultName)
        {
            _names.Add(Entity.InvalidEntity, defaultName);
        }

        #region Cloning Interface.

        public NameDB(NameDB nameDB)
        {
            _names = new Dictionary<Entity, string>(nameDB._names);
        }

        public override object Clone()
        {
            return new NameDB(this);
        }

        #endregion

        [PublicAPI]
        public string GetName(Entity requestingFaction)
        {
            string name;
            if (!_names.TryGetValue(requestingFaction, out name))
            {
                // Entry not found for the specific entity.
                // Return the default name.
                name = _names[Entity.InvalidEntity];
            }
            return name;
        }

        [PublicAPI]
        public void SetName(Entity requestingFaction, string specifiedName)
        {
            if (_names.ContainsKey(requestingFaction))
            {
                _names[requestingFaction] = specifiedName;
            }
            else
            {
                _names.Add(requestingFaction, specifiedName);
            }
        }
    }
}
