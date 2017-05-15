using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace Pulsar4X.ECSLib
{
    /// <summary>
    /// Provides a common API for formatting messages and retrieving info from formatted messages
    /// </summary>
    public static class MessageFormatter
    {
        private static readonly Regex _guidRegex = new Regex("^(?:([\\w]+);)");
        private static readonly Regex _guidListRegex = new Regex("^(?:([\\w]+),?)+;");
        private static readonly Regex _messageTypeRegex = new Regex("^(\\d+);");
        private static readonly Regex _authTokenRegex = new Regex("^(\\w+\n\\w+)\n");

        /// <summary>
        /// Retrieves a Guid from the message.
        /// Strips the detected Guid from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static bool TryGetGuid(ref string message, out Guid guid)
        {
            guid = Guid.Empty;
            Match match = _guidRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            guid = Guid.Parse(match.Groups[1].Captures[0].Value);

            message = message.Substring(match.Length);
            return true;
        }

        /// <summary>
        /// Retrieves a list of of Guid's from the message.
        /// Strips the detected guid list from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static bool TryGetGuidList(ref string message, out List<Guid> guidList)
        {
            guidList = new List<Guid>();

            // Detect if the passed message has a CSV GuidList at the beginning
            Match match = _guidListRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            foreach (Capture capture in match.Groups[1].Captures)
            {
                guidList.Add(Guid.Parse(capture.Value));
            }

            // Remove the captured guidList from the message.
            message = message.Substring(match.Length);
            return true;
        }
        

        /// <summary>
        /// Converts a list of Guids to a string formatted to be read by TryGetGuidList
        /// </summary>
        [PublicAPI]
        public static string GuidListToString([NotNull] IEnumerable<Guid> guidList)
        {
            if (guidList == null)
            {
                throw new ArgumentNullException(nameof(guidList));
            }

            var builder = new StringBuilder();
            foreach (Guid guid in guidList)
            {
                builder.Append($"{guid:N},");
            }
            builder.Replace(",", ";", builder.Length - 1, 1);
            return builder.ToString();
        }

        /// <summary>
        /// Returns a validly formatted header with the provided messageType and authToken.
        /// </summary>
        /// <remarks>
        /// Use this function to construct your message headers so the UI doesn't have to worry about how the ECSLib expects the header to be formatted.
        /// </remarks>
        [PublicAPI]
        public static string GetMessageHeader(IncomingMessageType messageType, [NotNull] AuthenticationToken authToken)
        {
            if (authToken == null)
            {
                throw new ArgumentNullException(nameof(authToken));
            }
            if (!Enum.IsDefined(typeof(IncomingMessageType), messageType))
            {
                throw new InvalidEnumArgumentException(nameof(messageType), (int)messageType, typeof(IncomingMessageType));
            }

            return $"{(int)messageType};{authToken}\n";
        }

        /// <summary>
        /// Retrieves the OutgoingMessageType from the provided message.
        /// Strips the OutgoingMessageType from the message for further processing.
        /// </summary>
        [PublicAPI]
        public static bool TryGetOutgoingMessageType(ref string message, out OutgoingMessageType messageType)
        {
            messageType = OutgoingMessageType.Invalid;
            Match match = _messageTypeRegex.Match(message);
            if (!match.Success || !Enum.TryParse(match.Groups[1].Captures[0].Value, out messageType))
            {
                return false;
            }

            // Strip the OutgoingMessageType from the message.
            message = message.Substring(match.Length);
            return true;
        }

        /// <summary>
        /// Helper function to create a fully-qualified message.
        /// </summary>
        [PublicAPI]
        public static string GetMessage(IncomingMessageType messageType, [NotNull] AuthenticationToken authToken, Guid guid, string message = "")
        {
            string finalMessage = GetMessageHeader(messageType, authToken);
            finalMessage += $"{guid:N};";
            finalMessage += message;
            return finalMessage;
        }

        /// <summary>
        /// Helper function to create a fully-qualified message.
        /// </summary>
        [PublicAPI]
        public static string GetMessage(IncomingMessageType messageType, [NotNull] AuthenticationToken authToken, [NotNull] IEnumerable<Guid> guidList, string message = "")
        {
            string finalMessage = GetMessageHeader(messageType, authToken);
            finalMessage += GuidListToString(guidList);
            finalMessage += message;
            return finalMessage;
        }

        /// <summary>
        /// Retrieves a correctly formatted OutgoingMessageHeader. Use this so if the header format changes, the code wont have to.
        /// </summary>
        internal static string GetOutgoingMessageHeader(OutgoingMessageType messageType) => $"{(int)messageType};";

        /// <summary>
        /// Retrieves the IncomingMessageType and AuthenticationToken from the provided message.
        /// Strips the header from the message for additional processing.
        /// </summary>
        internal static bool TryDeconstructHeader(ref string message, out IncomingMessageType messageType, out AuthenticationToken authToken)
        {
            string originalMessage = message;
            authToken = null;

            if (TryGetIncomingMessageType(ref message, out messageType) && TryGetAuthToken(ref message, out authToken))
            {
                return true;
            }

            // Invalid Header. Reconstruct original message.
            message = originalMessage;
            return false;
        }

        private static bool TryGetIncomingMessageType(ref string message, out IncomingMessageType messageType)
        {
            messageType = IncomingMessageType.Invalid;
            Match match = _messageTypeRegex.Match(message);
            if (!match.Success || !Enum.TryParse(match.Groups[1].Captures[0].Value, out messageType))
            {
                return false;
            }

            // Strip the IncomingMessageType from the message.
            message = message.Substring(match.Length);
            return true;
        }

        private static bool TryGetAuthToken(ref string message, out AuthenticationToken authToken)
        {
            authToken = null;
            Match match = _authTokenRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            authToken = new AuthenticationToken(match.Groups[1].Captures[0].Value);
            message = message.Substring(match.Length);
            return true;
        }
    }
}
