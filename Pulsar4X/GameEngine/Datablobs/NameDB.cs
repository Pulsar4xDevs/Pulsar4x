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
    public class NameDB : BaseDataBlob, ISensorCloneMethod
    {

        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        [JsonProperty("Names")]
        private Dictionary<int, string> _names = new ();

        [PublicAPI]
        public string DefaultName => _names[-1];

        public string OwnersName
        {
            get
            {
                if (_names.ContainsKey(OwningEntity.FactionOwnerID))
                    return _names[OwningEntity.FactionOwnerID];
                else return DefaultName;
            }
        }

        public NameDB() { _names.Add(-1, "Un-Named");}

        public NameDB(string defaultName)
        {
            _names.Add(-1, defaultName);
        }

        public NameDB(string defaultName, int factionID, string factionsName)
        {
            _names.Add(-1, defaultName);
            _names.Add(factionID, factionsName);
        }

        #region Cloning Interface.

        public NameDB(NameDB nameDB)
        {
            _names = new Dictionary<int, string>(nameDB._names);
        }

        public override object Clone()
        {
            return new NameDB(this);
        }

        #endregion

        [PublicAPI]
        public string GetName(int requestingFaction)
        {
            if (!_names.TryGetValue(requestingFaction, out var name))
            {
                name = OwnersName;
                SetName(requestingFaction, name);
            }
            return name;
        }

        [PublicAPI]
        public string GetName(Entity requestingFaction)
        {
            return GetName(requestingFaction.Id);
        }


        [PublicAPI]
        public void SetName(int requestingFaction, string specifiedName)
        {
            _names[requestingFaction] = specifiedName;
        }

        public BaseDataBlob SensorClone(SensorInfoDB sensorInfo)
        {
            return new NameDB(this, sensorInfo);
        }

        public void SensorUpdate(SensorInfoDB sensorInfo)
        {
            //do nothing for this.
        }

        NameDB(NameDB db, SensorInfoDB sensorInfo)
        {
            _names.Add(-1, db.DefaultName);
            _names[sensorInfo.Faction.Id] = db.GetName(sensorInfo.Faction.Id);
        }
    }
}