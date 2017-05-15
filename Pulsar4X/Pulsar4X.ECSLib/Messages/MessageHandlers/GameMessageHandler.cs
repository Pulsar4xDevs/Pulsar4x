using System.Text;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Handles simple game messages.
    /// </summary>
    internal class GameMessageHandler : IMessageHandler
    {
        public bool HandleMessage(Game game, IncomingMessageType messageType, AuthenticationToken authToken, string message)
        {
            switch (messageType)
            {
                case IncomingMessageType.Exit:
                    game.ExitRequested = true;
                    return true;
                case IncomingMessageType.ExecutePulse:
                    // TODO: Pulse length parsing
                    game.GameLoop.TimeStep();
                    return true;
                case IncomingMessageType.StartRealTime:
                    float timeMultiplier;
                    if (float.TryParse(message, out timeMultiplier))
                    {
                        game.GameLoop.TimeMultiplier = timeMultiplier;
                    }
                    game.GameLoop.StartTime();
                    return true;
                case IncomingMessageType.StopRealTime:
                    game.GameLoop.PauseTime();
                    return true;
                case IncomingMessageType.Echo:
                    game.MessagePump.EnqueueOutgoingMessage(OutgoingMessageType.Echo, message);
                    return true;

                // This message may be getting too complex for this handler.
                case IncomingMessageType.GalaxyQuery:
                    var systemGuids = new StringBuilder();
                    foreach (StarSystem starSystem in game.GetSystems(authToken))
                    {
                        systemGuids.Append($"{starSystem.Guid:N},");
                    }

                    game.MessagePump.EnqueueOutgoingMessage(OutgoingMessageType.GalaxyResponse, systemGuids.ToString(0, systemGuids.Length - 1));
                    return true;

                default: return false;
            }
        }
    }
}