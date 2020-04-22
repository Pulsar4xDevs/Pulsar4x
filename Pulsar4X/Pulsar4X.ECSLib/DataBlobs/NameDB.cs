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
        private readonly Dictionary<Guid, string> _names = new Dictionary<Guid, string>();

        [PublicAPI]
        public string DefaultName => _names[Guid.Empty];

        public NameDB() { }

        public NameDB(string defaultName)
        {
            _names.Add(Guid.Empty, defaultName);
        }

        public NameDB(string defaultName, Guid factionID, string factionsName)
        {
            _names.Add(factionID, factionsName);
        }

        #region Cloning Interface.

        public NameDB(NameDB nameDB)
        {
            _names = new Dictionary<Guid, string>(nameDB._names);
        }

        public override object Clone()
        {
            return new NameDB(this);
        }

        #endregion

        [PublicAPI]
        public string GetName(Guid requestingFaction)
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

        [PublicAPI]
        public string GetName(Entity requestingFaction)
        {
            string name;
            if (!_names.TryGetValue(requestingFaction.Guid, out name))
            {
                // Entry not found for the specific entity.
                // Return guid instead. TODO: call an automatic naming function
                name = OwningEntity.Guid.ToString();
                SetName(requestingFaction.Guid, name);

            }
            return name;
        }

        /// <summary>
        /// returns the name but no longer checks the auth. needs rewriting or getting rid of. 
        /// </summary>
        /// <returns>The name.</returns>
        /// <param name="requestingFaction">Requesting faction.</param>
        /// <param name="game">Game.</param>
        /// <param name="auth">Auth.</param>
        [Obsolete]
        public string GetName(Guid requestingFaction, Game game, AuthenticationToken auth)
        {
            /*
            if (game.GetPlayerForToken(auth).AccessRoles[requestingFaction] < AccessRole.Intelligence)
                requestingFaction = Entity.InvalidEntity;*/
            return GetName(requestingFaction);
        }

        [PublicAPI]
        public void SetName(Guid requestingFaction, string specifiedName)
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
                hash = Misc.ValueHash(item.Key, hash);
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
            _names.Add(Guid.Empty, db.DefaultName);
            _names[sensorInfo.Faction.Guid] = db.GetName(sensorInfo.Faction.Guid);
        }
    }
}