using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if LOG4NET_ENABLED
using log4net.Config;
using log4net;
#endif

namespace Pulsar4X.Entities
{
    public class MessageEntry
    {
        /// <summary>
        /// Message types that will be printed to the event log.
        /// </summary>
        public enum MessageType
        {
            ColonyLacksCI,
            ColonyLacksMinerals,

            ContactNew,
            ContactUpdate,
            ContactLost,

            FailureToLoad,

            Firing,
            FiringHit,
            FiringMissed,
            FiringNoAvailableOrdnance,
            FiringNoLoadedOrdnance,
            FiringRecharging,
            FiringZeroHitChance,

            LaunchTubeReloaded,
            LaunchTubeNoOrdnanceToReload,

            MissileHit,
            MissileIntercepted,
            MissileInterceptEvent,
            MissileLostFireControl,
            MissileLostTracking,
            MissileMissed,
            MissileOutOfFuel,

            NoGeoSurveyTarget,
            NoGravSurveyTarget,

            OrdersCompleted,
            OrdersNotCompleted,

            PopulationDamage,
            PotentialFleetInterception,

            ShieldRecharge,

            ShipDamage,
            ShipDamageReport,

            Error,
            Count
        }

#if LOG4NET_ENABLED
        /// <summary>
        /// Ship Logger:
        /// </summary>
        public static readonly ILog logger = LogManager.GetLogger(typeof(MessageEntry));
#endif

        /// <summary>
        /// What specific type of message is this?
        /// </summary>
        public MessageType TypeOf { get; set; }

        /// <summary>
        /// which starsystem does this message occur in.
        /// </summary>
        public StarSystem Location { get; set; }

        /// <summary>
        /// Which tg/planet/pop are we referencing?
        /// </summary>
        public StarSystemEntity entity { get; set; }

        /// <summary>
        /// When was this message sent?
        /// </summary>
        public DateTime TimeOfMessage { get; set; }

        /// <summary>
        /// How long since the last time increment?
        /// </summary>
        public int TimeSlice { get; set; }
        /// <summary>
        /// Text of the message for the log.
        /// </summary>
        public String Text { get; set; }


        /// <summary>
        /// MessageEntry constructs a specific message with the relevant location, time, and entity(if applicable). 
        /// </summary>
        /// <param name="Loc">Starsystem message is from.</param>
        /// <param name="Ref">Starsystementity the message refers to.</param>
        /// <param name="Time">Game time of message.</param>
        /// <param name="timeSlice">Time since last increment.</param>
        /// <param name="text">text of the message.</param>
        public MessageEntry(MessageType Type, StarSystem Loc, StarSystemEntity Ref, DateTime Time, int timeSlice, string text)
        {
            TypeOf = Type;
            Location = Loc;
            entity = Ref;
            TimeOfMessage = Time;
            TimeSlice = timeSlice;
            Text = text;


#if LOG4NET_ENABLED
            if (TypeOf == MessageType.Error)
            {
                String Entry = String.Format("Faction Message Logging Error: Type:{0} | Time:{1} | Location:{2} - TimeSlice:{3}: Text:{4}\n", TypeOf, TimeOfMessage, Location, TimeSlice, Text);
                logger.Debug(Entry);
            }
#endif
        }
    }
}
