using Newtonsoft.Json;

namespace Pulsar4X.ECSLib
{
    public class MissileLauncherAtbDB : BaseDataBlob
    {
        [JsonProperty]
        public double MissileSize { get; internal set; }

        [JsonProperty]
        public double ReloadRate { get; internal set; }

        public MissileLauncherAtbDB()
        {
        }

        public MissileLauncherAtbDB(double missileSize)
        {
            MissileSize = missileSize;
        }

        public MissileLauncherAtbDB(MissileLauncherAtbDB db)
        {
            MissileSize = db.MissileSize;
        }

        public override object Clone()
        {
            return new MissileLauncherAtbDB(this);
        }
    }
}