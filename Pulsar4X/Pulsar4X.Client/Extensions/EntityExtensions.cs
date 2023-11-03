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

        public static string GetFactionName(this Entity entity)
        {
            if(entity.FactionOwnerID == Game.NeutralFactionId)
            {
                return "Neutral";
            }
            else if(entity.FactionOwnerID != -1 && entity.Manager != null)
            {
                var ownerFaction = entity.Manager.Game.Factions[entity.FactionOwnerID];
                return ownerFaction.GetDataBlob<NameDB>().OwnersName;
            }
            else
            {
                return entity.FactionOwnerID.ToString();
            }
        }
    }
}