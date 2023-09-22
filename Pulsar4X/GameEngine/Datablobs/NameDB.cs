using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using Pulsar4X.Interfaces;
using Pulsar4X.Engine;
using Pulsar4X.Extensions;

namespace Pulsar4X.Datablobs
{
    [DebuggerDisplay("{" + nameof(OwnersName) + "}")]
    public class NameDB : BaseDataBlob, ISensorCloneMethod, IGetValuesHash
    {

        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        [JsonProperty]
        private readonly Dictionary<string, string> _names = new ();

        [PublicAPI]
        public string DefaultName => _names[String.Empty];

        public string OwnersName
        {
            get
            {
                if (_names.ContainsKey(OwningEntity.FactionOwnerID))
                    return _names[OwningEntity.FactionOwnerID];
                else return DefaultName;
            }
        }

        public NameDB() { _names.Add(String.Empty, "Un-Named");}

        public NameDB(string defaultName)
        {
            _names.Add(String.Empty, defaultName);
        }

        public NameDB(string defaultName, string factionID, string factionsName)
        {
            _names.Add(String.Empty, defaultName);
            _names.Add(factionID, factionsName);
        }

        #region Cloning Interface.

        public NameDB(NameDB nameDB)
        {
            _names = new Dictionary<string, string>(nameDB._names);
        }

        public override object Clone()
        {
            return new NameDB(this);
        }

        #endregion

        [PublicAPI]
        public string GetName(string requestingFaction)
        {
            if (!_names.TryGetValue(requestingFaction, out var name))
            {
                name = OwningEntity.Guid.ToString();
                SetName(requestingFaction, name);
            }
            return name;
        }

        [PublicAPI]
        public string GetName(Entity requestingFaction)
        {
            return GetName(requestingFaction.Guid);
        }


        [PublicAPI]
        public void SetName(string requestingFaction, string specifiedName)
        {
            _names[requestingFaction] = specifiedName;
        }

        public BaseDataBlob SensorClone(SensorInfoDB sensorInfo)
        {
            return new NameDB(this, sensorInfo);
        }

        public int GetValueCompareHash(int hash = 17)
        {
            foreach (var item in _names)
            {
                hash = ObjectExtensions.ValueHash(item.Key, hash);
                hash = ObjectExtensions.ValueHash(item.Value, hash);
            }
            return hash;
        }

        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            //do nothing for this.
        }

        NameDB(NameDB db, SensorInfoDB sensorInfo)
        {
            _names.Add(String.Empty, db.DefaultName);
            _names[sensorInfo.Faction.Guid] = db.GetName(sensorInfo.Faction.Guid);
        }
    }
}