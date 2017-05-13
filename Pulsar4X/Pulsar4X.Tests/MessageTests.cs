using System;
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
        public void ValidHeaderManipulation()
        {
            var messageType = IncomingMessageType.Echo;
            var authToken = new AuthenticationToken(Guid.NewGuid(), "hunter2");

            string message = MessagePump.GetMessageHeader(messageType, authToken);

            IncomingMessageType testMessageType;
            AuthenticationToken testAuthToken;

            MessagePump.TryDeconstructHeader(ref message, out testMessageType, out testAuthToken);
            Assert.IsEmpty(message);
            Assert.AreEqual(messageType, testMessageType);
            Assert.AreEqual(authToken, testAuthToken);

            var outgoingMT = OutgoingMessageType.Echo;
            message = MessagePump.GetOutgoingMessageHeader(outgoingMT);

            OutgoingMessageType testOutgoingMT;
            Assert.IsTrue(MessagePump.TryGetOutgoingMessageType(ref message, out testOutgoingMT));
            Assert.IsEmpty(message);
            Assert.AreEqual(outgoingMT, testOutgoingMT);
        }

        [Test]
        public void InvalidHeaderManipulation()
        {
            var messageType = (IncomingMessageType)(-1);
            var authToken = new AuthenticationToken(Guid.NewGuid(), "hunter2");

            string header = MessagePump.GetMessageHeader(messageType, authToken);

            IncomingMessageType testMessageType;
            AuthenticationToken testAuthToken;
            Assert.IsFalse(MessagePump.TryDeconstructHeader(ref header, out testMessageType, out testAuthToken));

            header = MessagePump.GetMessageHeader(IncomingMessageType.Echo, authToken);
            header = header.Substring(0, header.Length - 2);

            Assert.IsFalse(MessagePump.TryDeconstructHeader(ref header, out testMessageType, out testAuthToken));

            var outgoingMT = (OutgoingMessageType)(-1);
            header = MessagePump.GetOutgoingMessageHeader(outgoingMT);
            Assert.IsFalse(MessagePump.TryGetOutgoingMessageType(ref header, out outgoingMT));
        }


        [Test]
        public void MessageDispatcher()
        {
            
        }
    }
}
