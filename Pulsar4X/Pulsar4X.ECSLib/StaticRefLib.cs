using System;
using System.Threading;

namespace Pulsar4X.ECSLib
{
    public static class StaticRefLib
    {
        public static Game Game { get; private set; }
        public static StaticDataStore StaticData { get; internal set; }
        internal static ProcessorManager ProcessorManager { get; private set; }
        public static DateTime CurrentDateTime { get { return GamePulse.GameGlobalDateTime; } }
        public static EventLog EventLog { get; private set; }
        internal static MasterTimePulse GamePulse { get; private set; }

        public static IOrderHandler OrderHandler { get; private set; }
        
        /// <summary>
        /// this is used to marshal events to the UI thread. 
        /// </summary>
        public static SynchronizationContext SyncContext { get; private set; }

        public static GameSettings GameSettings { get; internal set; }

        internal static void SetEventlog(EventLog eventLog)
        {
            EventLog = eventLog; 
        }

        public static void Setup(Game game)
        {
            Game = game;
            StaticData = game.StaticData;
            ProcessorManager = new ProcessorManager(game);
            GamePulse = new MasterTimePulse(game);
            EventLog = new EventLog(game);
            SyncContext = SynchronizationContext.Current;
            OrderHandler = game.OrderHandler;
        }
    }
}
