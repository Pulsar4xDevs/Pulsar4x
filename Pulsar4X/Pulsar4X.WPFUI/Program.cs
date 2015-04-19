using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI
{
    static class Program
    {
        public static void PulsarMain()
        {
            /* Stuff to replace */
            Game game = new Game();
            Entity playerFaction = game.GlobalManager.GetFirstEntityWithDataBlob<FactionDB>();
            if (playerFaction == Entity.GetInvalidEntity())
                playerFaction = ECSLib.Factories.FactionFactory.CreateFaction(game.GlobalManager, "playerFaction");
            game.EngineComms.AddFaction(playerFaction);
            Entity faction = game.EngineComms.FirstOrDefault().Faction; //just get the first one for now, till we've got ui to select.
            /* Stuff to replace */

            UIComms uicomms = new UIComms(game.EngineComms, faction);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => uicomms.CheckEngineMessageQueue()));
        }
    }
}
