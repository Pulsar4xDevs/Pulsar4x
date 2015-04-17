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
                    
            Game game = new Game();
            Entity faction = game.EngineComms.FirstOrDefault().Faction; //just get the first one for now, till we've got ui to select.
            
            UI_Comms uicomms = new UI_Comms(game.EngineComms, faction);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => uicomms.CheckEngineMessageQueue()));

            
        }
    }
}
