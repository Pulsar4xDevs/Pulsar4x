using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pulsar4X.ECSLib;
using Pulsar4X.ECSLib.DataBlobs;

namespace Pulsar4X.Tests
{
    using NUnit.Framework;

    [TestFixture, Description("Tests for the Engine <-> UI Communications, including of the MessageBook class.")]
    class EngineCommsTests
    {
        Game game;
        MessageBook messageBook;
        int fakeFaction = 42;

         [SetUp]
        public void Init()
        {
            game = new Game();
            game.EngineComms.AddFaction(fakeFaction);
            messageBook = game.EngineComms.RequestMessagebook(fakeFaction);
        }

        [TearDown]
        public void Cleanup()
        {
            messageBook = null;
            game = null;
        }

        [Test]
        public void TestMessageBook()
        {
            Assert.AreEqual(fakeFaction, messageBook.FactionID);


            messageBook.InMessageQueue.Enqueue(new Message(Message.MessageType.Echo, 42));
            Assert.AreEqual(1, messageBook.InMessageQueue.Count);

            Message message = null;
            Assert.IsTrue(messageBook.InMessageQueue.TryDequeue(out message));
            Assert.IsNotNull(message);
            Assert.AreEqual(42, Convert.ToInt32(message._data));
            Assert.AreEqual(0, messageBook.InMessageQueue.Count);


            messageBook.OutMessageQueue.Enqueue(message);
            Assert.AreEqual(1, messageBook.OutMessageQueue.Count);

            message = null;
            Assert.IsTrue(messageBook.OutMessageQueue.TryDequeue(out message));
            Assert.IsNotNull(message);
            Assert.AreEqual(42, Convert.ToInt32(message._data));
            Assert.AreEqual(0, messageBook.OutMessageQueue.Count);
        }
    }
}
