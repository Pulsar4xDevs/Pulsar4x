using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Pulsar4X.ECSLib;

namespace Pulsar4X.WPFUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static Game GameInstance;

        App()
        {
            /* Stuff to replace */
            Game game = new Game();
            Entity playerFaction = game.GlobalManager.GetFirstEntityWithDataBlob<FactionDB>();
            if (playerFaction == Entity.GetInvalidEntity())
                playerFaction = FactionFactory.CreateFaction(game.GlobalManager, "playerFaction");
            game.EngineComms.AddFaction(playerFaction);
            Entity faction = game.EngineComms.FirstOrDefault().Faction; //just get the first one for now, till we've got ui to select.
            /* Stuff to replace */

            UIComms uicomms = new UIComms(game.EngineComms, faction);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => uicomms.CheckEngineMessageQueue()));
        }
    }
}
