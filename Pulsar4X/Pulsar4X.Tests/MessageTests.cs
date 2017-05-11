using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Message Tests")]
    class MessageTests
    {
        private static readonly MessagePump MessagePump = new MessagePump();

        [Test]
        public void MessagePumpQueueOperations()
        {
            Assert.AreEqual(MessagePump.IncomingMessageCount, 0);
            Assert.AreEqual(MessagePump.OutgoingMessageCount, 0);

            string outString;
            Assert.IsFalse(MessagePump.TryDequeueIncomingMessage(out outString));
            Assert.IsFalse(MessagePump.TryDequeueOutgoingMessage(out outString));

            string incomingMessage = "TestIncomingMessage";
            MessagePump.EnqueueIncomingMessage(incomingMessage);
            Assert.AreEqual(MessagePump.IncomingMessageCount, 1);

            string outgoingMessage = "TestOutgoingMessage";
            MessagePump.EnqueueOutgoingMessage(outgoingMessage);
            Assert.AreEqual(MessagePump.OutgoingMessageCount, 1);

            Assert.IsTrue(MessagePump.TryDequeueIncomingMessage(out outString));
            Assert.AreEqual(outString, incomingMessage);
            Assert.AreEqual(MessagePump.IncomingMessageCount, 0);
            
            Assert.IsTrue(MessagePump.TryDequeueOutgoingMessage(out outString));
            Assert.AreEqual(outString, outgoingMessage);
            Assert.AreEqual(MessagePump.OutgoingMessageCount, 0);
        }

        [Test]
        public void MessageDispatcher()
        {
            
        }
    }
}
