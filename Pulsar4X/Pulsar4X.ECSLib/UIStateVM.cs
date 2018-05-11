using System;
namespace Pulsar4X.ECSLib
{
    //makes internal functions and data public. 
    public class UIStateVM
    {
        Game _game;
        public Entity FactionEntity { get; set; }


        public UIStateVM(Game game)
        {
            _game = game;
        }

        //TODO: should I out a message here?
        public bool TrySetFactionEntityByName(string factionName, string password)
        {
            
            Entity factionEntity;
            if (NameLookup.TryGetFirstEntityWithName(_game.GlobalManager, factionName, out factionEntity))
            {
                if (AuthProcessor.Validate(factionEntity, password))
                {
                    FactionEntity = factionEntity;
                    return true;
                }
            }
            return false;
        }
    }
}
