using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace Pulsar4X.ECSLib
{
    [DebuggerDisplay("{" + nameof(DefaultName) + "}")]
    public class NameDB : BaseDataBlob, ISensorCloneMethod, IGetValuesHash
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
                // Return guid instead. TODO: call an automatic naming function
                name = OwningEntity.Guid.ToString();
                SetName(requestingFaction, name);

            }
            return name;
        }

        public string GetName(Entity requestingFaction, Game game, AuthenticationToken auth)
        {

            if (game.GetPlayerForToken(auth).AccessRoles[requestingFaction] < AccessRole.Intelligence)
                requestingFaction = Entity.InvalidEntity;
            return GetName(requestingFaction);
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

        public BaseDataBlob SensorClone(SensorInfoDB sensorInfo)
        {
            return new NameDB(this, sensorInfo);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in _names)
            {
                hash = Misc.ValueHash(item.Key.Guid, hash);
                hash = Misc.ValueHash(item.Value, hash);
            }
            return hash;
        }

        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            //do nothing for this. 
        }

        NameDB(NameDB db, SensorInfoDB sensorInfo)
        {            
            _names.Add(Entity.InvalidEntity, db.DefaultName);
            _names[sensorInfo.Faction] = db.GetName(sensorInfo.Faction);
        }
    }
}