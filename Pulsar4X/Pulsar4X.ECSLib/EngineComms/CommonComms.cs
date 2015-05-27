using System;
using System.Globalization;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    public class LibProcessLayer : IProcessLayer
    {
        /// <summary>
        /// Used by the Library to check all servers for messages.
        /// Called by Game.MainGameLoop
        /// Returns true if further messages await processing.
        /// </summary>
        public bool ProcessMessages()
        {
            bool messagesLeft = false;
            foreach (IServerTransportLayer serverLayer in Game.Instance.Servers)
            {
                messagesLeft = serverLayer.ProcessMessage() || messagesLeft;
            }
            return messagesLeft;
        }

        public void ProcessNewGameMessage(Message currentMessage)
        {
            SendStatusUpdate(CommandByte.NewGame, StatusByte.Started);

            string data = currentMessage.Data.ToString();
            bool forceNewFlag = data.StartsWith("-fn");

            if (Game.Instance != null && Game.Instance.IsLoaded && !forceNewFlag)
            {
                SendStatusUpdate(CommandByte.NewGame, StatusByte.Error, "Already Loaded.");
                return;
            }

            if (forceNewFlag)
            {
                data = data.Substring(4);
            }

            string gameName = data.Substring(0, data.IndexOf(';'));

            Game game = new Game(this, gameName, data);

            try
            {
                Game.Instance.SaveGame.Save();
                Thread commsThread = new Thread(Game.Instance.MainGameLoop);
                commsThread.Start();
            }
            catch (Exception e)
            {
                SendStatusUpdate(CommandByte.NewGame, StatusByte.Failed, e.Message);
            }


            SendStatusUpdate(CommandByte.NewGame, StatusByte.Completed);
        }

        public void ProcessSaveMessage(Message currentMessage)
        {
            SendStatusUpdate(CommandByte.SaveGame, StatusByte.Started);
            try
            {
                Game.Instance.SaveGame.Save();
            }
            catch (Exception e)
            {
                SendStatusUpdate(CommandByte.SaveGame, StatusByte.Failed, e.Message);
            }
            SendStatusUpdate(CommandByte.SaveGame, StatusByte.Completed);
        }

        public void ProcessLoadMessage(Message currentMessage)
        {
            string data = currentMessage.Data.ToString();

            SaveGame save = new SaveGame(data);

            SendStatusUpdate(CommandByte.LoadGame, StatusByte.Started);
            try
            {
                save.Load(data);
            }
            catch (Exception e)
            {
                SendStatusUpdate(CommandByte.LoadGame, StatusByte.Failed, e.Message);
                return;
            }

            SendStatusUpdate(CommandByte.LoadGame, StatusByte.Completed);
        }

        public void ProcessPulseMessage(Message currentMessage)
        {
            if (!(currentMessage.Data is int))
            {
                SendStatusUpdate(CommandByte.Pulse, StatusByte.Error, "Data is not a valid integer.");
                return;
            }

            int secondsDesired = (int)currentMessage.Data;
            int secondsCompleted;

            SendStatusUpdate(CommandByte.Pulse, StatusByte.Started);
            try
            {
                secondsCompleted = Game.Instance.AdvanceTime(secondsDesired);
            }
            catch (Exception e)
            {
                SendStatusUpdate(CommandByte.Pulse, StatusByte.Failed, e.Message);
                return;
            }

            if (secondsCompleted == secondsDesired)
            {
                SendStatusUpdate(CommandByte.Pulse, StatusByte.Completed, Game.Instance.CurrentDateTime.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                SendStatusUpdate(CommandByte.Pulse, StatusByte.Interrupted, Game.Instance.CurrentDateTime.ToString(CultureInfo.InvariantCulture) + "; " + Game.Instance.CurrentInterrupt.Reason);
            }
        }

        public void SendStatusUpdate(CommandByte command, StatusByte status, string data = null)
        {
            foreach (IServerTransportLayer serverLayer in Game.Instance.Servers)
            {
                serverLayer.SendStatusUpdate(command, status, data);
            }
        }

        public void ProcessQuitMessage(Message currentMessage)
        {
            Game.Instance.QuitMessageReceived = true;
            SendStatusUpdate(CommandByte.Quit, StatusByte.Completed);
        }
    }
}
