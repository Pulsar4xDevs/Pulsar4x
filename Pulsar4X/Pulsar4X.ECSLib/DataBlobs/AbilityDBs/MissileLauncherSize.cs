using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MissileLauncherSizeDB : BaseDataBlob
    {

        [JsonProperty]
        private double _missileLauncherSize;

        public double MissileLauncherSize { get { return _missileLauncherSize; } internal set { _missileLauncherSize = value; } }

        public MissileLauncherSizeDB()
        {
        }

        public MissileLauncherSizeDB(double missileSize)
        {
            _missileLauncherSize = missileSize;
        }

        public MissileLauncherSizeDB(MissileLauncherSizeDB db)
        {
            _missileLauncherSize = db.MissileLauncherSize;
        }

        public override object Clone()
        {
            return new MissileLauncherSizeDB(this);
        }
    }
}