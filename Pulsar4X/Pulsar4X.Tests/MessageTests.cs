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
            
            string outString;
            Assert.IsFalse(MessagePump.TryPeekIncomingMessage(out outString));
            Assert.IsFalse(MessagePump.TryPeekOutgoingMessage(out outString));
            Assert.IsFalse(MessagePump.TryDequeueIncomingMessage(out outString));
            Assert.IsFalse(MessagePump.TryDequeueOutgoingMessage(out outString));

            string incomingMessage = "TestIncomingMessage";
            MessagePump.EnqueueIncomingMessage(incomingMessage);

            string outgoingMessage = "TestOutgoingMessage";
            MessagePump.EnqueueOutgoingMessage(outgoingMessage);

            Assert.IsTrue(MessagePump.TryDequeueIncomingMessage(out outString));
            Assert.AreEqual(outString, incomingMessage);
            
            Assert.IsTrue(MessagePump.TryDequeueOutgoingMessage(out outString));
            Assert.AreEqual(outString, outgoingMessage);
            
            Assert.IsFalse(MessagePump.TryPeekIncomingMessage(out outString));
            Assert.IsFalse(MessagePump.TryPeekOutgoingMessage(out outString));
            Assert.IsFalse(MessagePump.TryDequeueIncomingMessage(out outString));
            Assert.IsFalse(MessagePump.TryDequeueOutgoingMessage(out outString));
        }

        [Test]
        public void MessageManipulationOperations()
        {
            
        }


        [Test]
        public void MessageDispatcher()
        {
            
        }
    }
}
