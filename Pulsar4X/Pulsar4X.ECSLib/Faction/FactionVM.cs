using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    //makes internal functions and data public. 
    public class FactionVM
    {
        Game _game;

        public Entity FactionEntity { get; private set; }
    
        public FactionInfoDB FactionInfo { get; private set; }
        FactionOwnerDB _factionOwner;
        public List<StarSystem> KnownSystems = new List<StarSystem>();
        public SystemMap_DrawableVM SystemMap = new SystemMap_DrawableVM();
        public FactionVM(Game game)
        {
            _game = game;
        }


        public void CreateDefaultFaction(string name, string password)
        {
            Entity factionEntity = DefaultStartFactory.DefaultHumans(_game, name);
            AuthProcessor.StorePasswordAsHash(_game, factionEntity, password);
            SetFaction(factionEntity);
        }

        //TODO: should I out a message here?
        public bool TrySetFaction(string factionName, string password)
        {
            
            Entity factionEntity;
            if (NameLookup.TryGetFirstEntityWithName(_game.GlobalManager, factionName, out factionEntity))
            {
                if (AuthProcessor.Validate(factionEntity, password))
                {
                    SetFaction(factionEntity);
                    return true;
                }
            }
            return false;
        }

        public bool TrySetFaction(Guid entityGuid, string password)
        {
            Entity factionEntity = _game.GlobalManager.GetLocalEntityByGuid(entityGuid);
            if (AuthProcessor.Validate(factionEntity, password))
            {
                SetFaction(factionEntity);
                return true;
            }
            return false;
        }

        void SetFaction(Entity factionEntity)
        {
            FactionEntity = factionEntity;
            FactionInfo = FactionEntity.GetDataBlob<FactionInfoDB>();
            foreach (var itemGuid in FactionInfo.KnownSystems)
            {
                KnownSystems.Add(_game.Systems[itemGuid]);
            }

            _factionOwner = factionEntity.GetDataBlob<FactionOwnerDB>();
            SystemMap.Initialise(null, _game.Systems[FactionInfo.KnownSystems[0]], factionEntity);
        }



    }
}
