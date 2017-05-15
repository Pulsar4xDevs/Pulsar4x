namespace Pulsar4X.ECSLib
{
    public enum IncomingMessageType
    {
        Invalid = 0,

        // Message Header Format:
        // "messageType;serializedAuthToken"

        // Message Format:
        // "messageHeader;message"

        Echo,

        /// <summary>
        /// Requests the game to exit.
        /// </summary>
        Exit, // message format ""

        ExecutePulse,   // message format: "pulseLengthMS"
        StartRealTime,  // message format: "realTimeMultiplier"
        StopRealTime,   // message format: ""

        /// <summary>
        /// Requests a list of dataBlobs that have been edited.
        /// ECSLib will respond with full dataBlob data of all entity dataBlobs
        /// </summary>
        EntityData, // message format: "entityGuid"

        /// <summary>
        /// Requests a list of Entities that have been edited in a system.
        /// ECSLib will respond with Guid's of all edited entities.
        /// </summary>
        EntityChangeList, // message format: "solarSystemGuid"

        /// <summary>
        /// Reads/Writes data from/to an Entity's Order Queue
        /// </summary>
        EntityOrdersQuery, // message format: "entityGuid"
        EntityOrdersWrite, // message format: "entityGuid{,entityGuid...};serializedOrder"
        EntityOrdersClear, // message format: "entityGuid{,entityGuid...}"

        /// <summary>
        /// Reads/Writes data from/to GameSettings
        /// </summary>
        SettingsQuery, // message format: ""
        SettingsWrite, // message format: "serializedSettings"

        /// <summary>
        /// Reads/Writes data from/to DataBlobs
        /// </summary>
        DataBlobWrite, // message format: "entityGuid;dataBlobTypeName;serializedDB"
        DataBlobQuery, // message format: "entityGuid;dataBlobTypeName"

        /// <summary>
        /// Reads/Writes the FactionAccess Roles.
        /// </summary>
        FactionAccessWrite, // message format: "factionEntityGuid;targetFactionGuid;targetFactionAccess"

        /// <summary>
        /// Requests a list of all known SolarSystems
        /// </summary>
        GalaxyQuery, // message format: ""

        /// <summary>
        /// Request a list of all Entities within a solarSystem.
        /// </summary>
        SolarSystemQuery, // message format: "systemGuid"
    }
}