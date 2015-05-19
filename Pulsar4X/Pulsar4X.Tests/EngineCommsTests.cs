using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests for the Engine <-> UI Communications, including of the MessageBook class.")]
    class EngineCommsTests
    {
        Game _game;
        MessageBook _messageBook;
        Entity _fakeFaction = Entity.InvalidEntity;

        [SetUp]
        public void Init()
        {
            _game = new Game();

            _fakeFaction = FactionFactory.CreateFaction(_game.GlobalManager, "Fake Faction");
            _game.EngineComms.AddFaction(_fakeFaction.Guid);
            _messageBook = _game.EngineComms.RequestMessagebook(_fakeFaction.Guid);
        }

        [TearDown]
        public void Cleanup()
        {
            _messageBook = null;
            _game = null;
        }

        [Test]
        public void TestMessageBook()
        {
            Assert.AreEqual(_fakeFaction.Guid, _messageBook.Faction);


            _messageBook.InMessageQueue.Enqueue(new Message(MessageType.Echo, 42));
            Assert.AreEqual(1, _messageBook.InMessageQueue.Count);

            Message message = null;
            Assert.IsTrue(_messageBook.InMessageQueue.TryDequeue(out message));
            Assert.IsNotNull(message);
            Assert.AreEqual(42, Convert.ToInt32(message.Data));
            Assert.AreEqual(0, _messageBook.InMessageQueue.Count);


            _messageBook.OutMessageQueue.Enqueue(message);
            Assert.AreEqual(1, _messageBook.OutMessageQueue.Count);

            message = null;
            Assert.IsTrue(_messageBook.OutMessageQueue.TryDequeue(out message));
            Assert.IsNotNull(message);
            Assert.AreEqual(42, Convert.ToInt32(message.Data));
            Assert.AreEqual(0, _messageBook.OutMessageQueue.Count);
        }
    }
}
