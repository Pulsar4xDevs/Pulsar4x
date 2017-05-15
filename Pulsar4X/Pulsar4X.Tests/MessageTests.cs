using System;
using System.Collections.Generic;
using System.ComponentModel;
using NUnit.Framework;
using Pulsar4X.ECSLib;

namespace Pulsar4X.Tests
{
    [TestFixture]
    [NUnit.Framework.Description("Message Tests")]
    internal class MessageTests
    {
        private static MessagePump _messagePump;

        [Test]
        public void MessagePumpQueueOperations()
        {
            _messagePump = new MessagePump();
            string outString;

            // Attempt to read empty queues.
            Assert.IsFalse(_messagePump.TryPeekIncomingMessage(out outString));
            Assert.IsFalse(_messagePump.TryPeekOutgoingMessage(out outString));
            Assert.IsFalse(_messagePump.TryDequeueIncomingMessage(out outString));
            Assert.IsFalse(_messagePump.TryDequeueOutgoingMessage(out outString));

            // Verify Queue operations
            string incomingMessage = "TestIncomingMessage";
            _messagePump.EnqueueMessage(incomingMessage);
            string outgoingMessage = "TestOutgoingMessage";
            _messagePump.EnqueueOutgoingMessage(OutgoingMessageType.Invalid, outgoingMessage);

            // Verify Dequeue operations
            Assert.IsTrue(_messagePump.TryDequeueIncomingMessage(out outString));
            Assert.AreEqual(outString, incomingMessage);
            Assert.IsTrue(_messagePump.TryDequeueOutgoingMessage(out outString));
            OutgoingMessageType outMessageTypeTest;
            Assert.IsTrue(MessagePump.TryGetOutgoingMessageType(ref outString, out outMessageTypeTest));
            Assert.AreEqual(outMessageTypeTest, OutgoingMessageType.Invalid);
            Assert.AreEqual(outString, outgoingMessage);

            // Verify Queues are now empty.
            Assert.IsFalse(_messagePump.TryPeekIncomingMessage(out outString));
            Assert.IsFalse(_messagePump.TryPeekOutgoingMessage(out outString));
            Assert.IsFalse(_messagePump.TryDequeueIncomingMessage(out outString));
            Assert.IsFalse(_messagePump.TryDequeueOutgoingMessage(out outString));

            // Verify extended public enqueue.
            var authToken = new AuthenticationToken(Guid.NewGuid(), "hunter2");
            string message = "This is a test.";
            _messagePump.EnqueueMessage(IncomingMessageType.Echo, authToken, message);
            Assert.IsTrue(_messagePump.TryDequeueIncomingMessage(out outString));
            IncomingMessageType incomingMessageTypeTest;
            AuthenticationToken authTokenTest;

            Assert.IsTrue(MessagePump.TryDeconstructHeader(ref outString, out incomingMessageTypeTest, out authTokenTest));
            Assert.AreEqual(authToken, authTokenTest);
            Assert.AreEqual(IncomingMessageType.Echo, incomingMessageTypeTest);
            Assert.AreEqual(message, outString);

        }

        [Test]
        public void ValidHeaderManipulation()
        {
            _messagePump = new MessagePump();

            // Retrieve a header for a message.
            var messageType = IncomingMessageType.Echo;
            var authToken = new AuthenticationToken(Guid.NewGuid(), "hunter2");
            string message = MessagePump.GetMessageHeader(messageType, authToken);

            // Decode the header we retrieved above, ensure equality.
            IncomingMessageType testMessageType;
            AuthenticationToken testAuthToken;
            MessagePump.TryDeconstructHeader(ref message, out testMessageType, out testAuthToken);
            Assert.IsEmpty(message);
            Assert.AreEqual(messageType, testMessageType);
            Assert.AreEqual(authToken, testAuthToken);

            // Ensure outgoingMessages get proper headers, and are proeprly decoded.
            _messagePump.EnqueueOutgoingMessage(OutgoingMessageType.Echo, "EchoTest");
            OutgoingMessageType outMessageTypeTest;

            Assert.True(_messagePump.TryPeekOutgoingMessage(out message));
            Assert.IsTrue(MessagePump.TryGetOutgoingMessageType(ref message, out outMessageTypeTest));
            Assert.AreEqual(message, "EchoTest");
            Assert.AreEqual(OutgoingMessageType.Echo, outMessageTypeTest);
        }

        [Test]
        public void InvalidHeaderManipulation()
        {
            _messagePump = new MessagePump();

            // Attempt to get a header for a message with an invalid IncomingMessageType
            var messageType = (IncomingMessageType)(-1);
            var authToken = new AuthenticationToken(Guid.NewGuid(), "hunter2");
            string message;
            Assert.Throws<InvalidEnumArgumentException>(() => message = MessagePump.GetMessageHeader(messageType, authToken));

            // Attempt to manually queue a message with a invalid IncomingMessageType.
            // NOTE: This is a valid situation the MessageDispatcher will have to deal with if VM sends a malformed message.
            string invalidMsg = $"-1;{authToken}";
            _messagePump.EnqueueMessage(invalidMsg);
            Assert.IsTrue(_messagePump.TryDequeueIncomingMessage(out message));

            IncomingMessageType messageTypeTest;
            AuthenticationToken authTokenTest;
            Assert.IsFalse(MessagePump.TryDeconstructHeader(ref message, out messageTypeTest, out authTokenTest));

            // Attempt to get a header for a null authToken.
            Assert.Throws<ArgumentNullException>(() => invalidMsg = MessagePump.GetMessageHeader(IncomingMessageType.Echo, null));

            // Attempt to manually queue a message with an invalid authToken
            // NOTE: This is a valid situation the MessageDispatcher will have to deal with if VM sends a malformed message.
            invalidMsg = "1;\n";
            _messagePump.EnqueueMessage(invalidMsg);
            Assert.IsTrue(_messagePump.TryDequeueIncomingMessage(out message));
            Assert.IsFalse(MessagePump.TryDeconstructHeader(ref message, out messageTypeTest, out authTokenTest));

            // Attempt to queue a message with an invalid OutgoingMessageType.
            Assert.Throws<InvalidEnumArgumentException>(() => _messagePump.EnqueueOutgoingMessage((OutgoingMessageType)(-1), "Invalid MessageType"));
            Assert.False(_messagePump.TryPeekOutgoingMessage(out message));

            // Attempt to get message type from a malformed OutgoingMessage
            // NOTE: This is a valid situation the VM will have to deal with if the ECSLib sends a malformed message.
            invalidMsg = "-1;";
            OutgoingMessageType outMessageTypeTest;
            Assert.False(MessagePump.TryGetOutgoingMessageType(ref invalidMsg, out outMessageTypeTest));
        }

        [Test]
        public void GuidListConversion()
        {
            // Generate 10 random Guids
            var guidList = new List<Guid>();
            for (int i = 0; i < 10; i++)
            {
                guidList.Add(Guid.NewGuid());
            }

            // Convert GuidList to string.
            string guidListString = MessagePump.GuidListToString(guidList);

            // Convert string to new GuidList
            List<Guid> retrievedGuids;
            Assert.IsTrue(MessagePump.TryGetGuidList(ref guidListString, out retrievedGuids));

            // Verify we got the same Guids back
            for (int i = 0; i < 10; i++)
            {
                Guid originalGuid = guidList[i];
                Guid parsedGuid = retrievedGuids[i];
                Assert.AreEqual(originalGuid, parsedGuid);
            }

            // Verify the GuidList was removed from the string (which was the whole string)
            Assert.IsEmpty(guidListString);

            // Ensure TryGetGuidList returns false with a bad format.
            foreach (Guid guid in guidList)
            {
                guidListString += $"{guid:B},";
            }
            guidListString.Substring(0, guidListString.Length - 1);
            guidListString += ';';
            Assert.IsFalse(MessagePump.TryGetGuidList(ref guidListString, out retrievedGuids));
        }

        [Test]
        public void GuidConversion()
        {
            // Generate new Guid
            Guid guid = Guid.NewGuid();
            // Convert it to a MessagePump Guid formatted string
            string guidString = $"{guid:N};";

            // Parse the Guid from the string.
            Guid parsedGuid;
            Assert.IsTrue(MessagePump.TryGetGuid(ref guidString, out parsedGuid));

            // Verify got the same Guid back.
            Assert.AreEqual(guid, parsedGuid);
            // Verify the Guid was removed from the string (which was the whole string)
            Assert.IsEmpty(guidString);

            // Ensure TryGetGuid returns false with a bad format.
            guidString = $"{guid:B};";
            Assert.IsFalse(MessagePump.TryGetGuid(ref guidString, out parsedGuid));
        }
    }
}
