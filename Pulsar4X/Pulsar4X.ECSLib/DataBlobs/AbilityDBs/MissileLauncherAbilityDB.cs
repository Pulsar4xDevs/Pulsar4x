using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MissileLauncherAbilityDB : BaseDataBlob
    {
        [JsonProperty]
        public double MissileSize { get; internal set; }

        [JsonProperty]
        public double ReloadRate { get; internal set; }

        public MissileLauncherAbilityDB()
        {
        }

        public MissileLauncherAbilityDB(double missileSize)
        {
            MissileSize = missileSize;
        }

        public MissileLauncherAbilityDB(MissileLauncherAbilityDB db)
        {
            MissileSize = db.MissileSize;
        }

        public override object Clone()
        {
            return new MissileLauncherAbilityDB(this);
        }
    }
}