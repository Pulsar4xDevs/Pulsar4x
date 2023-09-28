using Pulsar4X.Engine;
using Pulsar4X.Datablobs;

namespace Pulsar4X.SDL2UI
{
    public static class EntityExtensions
    {
        public static bool CanShowMiningTab(this Entity entity)
        {
            if(!entity.HasDataBlob<ColonyInfoDB>()) return false;
            if(!entity.HasDataBlob<MiningDB>()) return false;
            if(!entity.GetDataBlob<ColonyInfoDB>().PlanetEntity.HasDataBlob<MineralsDB>()) return false;
            if(!entity.HasDataBlob<VolumeStorageDB>()) return false;

            return true;
        }
    }
}