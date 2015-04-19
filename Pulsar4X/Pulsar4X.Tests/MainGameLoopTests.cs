using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib;

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
            var list = new List<BaseDataBlob>();
            Entity sdb = Entity.Create(game.GlobalManager, new List<BaseDataBlob>{new SpeciesDB( 1.0, 0.5, 1.5, 1.0, 0.5, 1.5, 22, 0, 44)});
            var pop = new JDictionary<Entity, double> {{sdb, 42}};

            list.Add(new ColonyInfoDB(pop, Entity.GetInvalidEntity()));
            Entity faction = Entity.Create(game.GlobalManager, list);
            game.EngineComms.AddFaction(faction);

            faction = Entity.Create(game.GlobalManager, list);
            game.EngineComms.AddFaction(faction);

            faction = Entity.Create(game.GlobalManager, list);
            game.EngineComms.AddFaction(faction);

            faction = Entity.Create(game.GlobalManager, list);
            game.EngineComms.AddFaction(faction);

            faction = Entity.Create(game.GlobalManager, list);
            game.EngineComms.AddFaction(faction);
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

            MessageBook mb0 = game.EngineComms.FirstOrDefault(); // lets try get the message book for the first faction.
            Assert.NotNull(mb0);

            // lets try an echo:
            mb0.InMessageQueue.Enqueue(new Message(Message.MessageType.Echo, 42));
            System.Threading.Thread.Sleep(100); // give the game time to echo!!
            Message message;
            Assert.IsTrue(mb0.OutMessageQueue.TryDequeue(out message), "Lib did not return echo message.");
            Assert.NotNull(message);
            Assert.AreEqual(42, Convert.ToInt32(message.Data));

            // now lets try quiting:
            mb0.InMessageQueue.Enqueue(new Message(Message.MessageType.Quit, null));
            gameThread.Join();
            System.Threading.Thread.Sleep(100); // give the game time to quit!!
            Assert.AreEqual(true, gameThread.ThreadState == System.Threading.ThreadState.Stopped); // has it stopped?
        }
    }
}
