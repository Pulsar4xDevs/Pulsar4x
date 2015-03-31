using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Basic Tests for the Main Game Loop.")]
    class MainGameLoopTests
    {
        Game game;
        System.Threading.Thread gameThread;

        [SetUp]
        public void Init()
        {
            game = new Game();

            // add some factions:
            List<BaseDataBlob> list = new List<BaseDataBlob>();
            SpeciesDB sdb = new SpeciesDB("Human", 1.0, 0.5, 1.5, 1.0, 0.5, 1.5, 22, 0, 44);
            Dictionary<SpeciesDB, double> pop = new Dictionary<SpeciesDB, double>();
            pop.Add(sdb, 42);

            list.Add(new PopulationDB(pop));
            int factionID = game.GlobalManager.CreateEntity(list);
            game.EngineComms.AddFaction(factionID);

            factionID = game.GlobalManager.CreateEntity(list);
            game.EngineComms.AddFaction(factionID);

            factionID = game.GlobalManager.CreateEntity(list);
            game.EngineComms.AddFaction(factionID);

            factionID = game.GlobalManager.CreateEntity(list);
            game.EngineComms.AddFaction(factionID);

            factionID = game.GlobalManager.CreateEntity(list);
            game.EngineComms.AddFaction(factionID);
        }

        [TearDown]
        public void Cleanup()
        {
            if (gameThread.IsAlive)
                gameThread.Abort();  // not good, but better safe then sorry :)

            game = null;
        }

        [Test]
        public void TestMainGameLoop()
        {
             // lets start the main game loop in a different thread:
             gameThread = new System.Threading.Thread(game.MainGameLoop);
             gameThread.Start();
             Assert.AreEqual(true, gameThread.ThreadState != System.Threading.ThreadState.Unstarted); // has it started?

             MessageBook mb0 = game.EngineComms.RequestMessagebook(0); // lets try get the message book for the first faction.
             Assert.NotNull(mb0);

             // lets try an echo:
             mb0.InMessageQueue.Enqueue(new Message(Message.MessageType.Echo, 42));
             System.Threading.Thread.Sleep(100); // give the game time to echo!!
             Message message = null;
             Assert.IsTrue(mb0.OutMessageQueue.TryDequeue(out message), "Lib did not return echo message.");
             Assert.NotNull(message);
             Assert.AreEqual(42, Convert.ToInt32(message._data));

             // now lets try quiting:
             mb0.InMessageQueue.Enqueue(new Message(Message.MessageType.Quit, null));
             gameThread.Join();
             System.Threading.Thread.Sleep(100); // give the game time to quit!!
             Assert.AreEqual(true, gameThread.ThreadState == System.Threading.ThreadState.Stopped); // has it stopped?
        }
    }
}
