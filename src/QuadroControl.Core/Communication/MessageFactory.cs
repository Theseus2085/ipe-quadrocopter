using QuadroControl.Core.Protocol;

namespace QuadroControl.Core.Communication
{
    /// <summary>
    /// Zentrale Fabrik für die sendefertigen UAVTalk-Hex-Strings der drei
    /// benötigten UAVObjects. Alle Nachrichten sind OHNE Checksumme — die
    /// hängt SchickeHexString() der Kurs-DLL automatisch an.
    /// </summary>
    public static class MessageFactory
    {
        /// <summary>
        /// ManualControlCommand-Nachricht (alle 9 Felder, feste Reihenfolge).
        /// Instanz-ID 00 00, Message-Type ObjectData.
        /// </summary>
        public static string BuildManualControlCommand(ManualControlCommandData command)
        {
            return UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData,
                ObjectIds.ManualControlCommand,
                null,
                command.ToPayload());
        }

        /// <summary>
        /// AccessoryDesired-Nachricht: accessoryVal = +1 (Arming) bzw. -1 (Disarming).
        /// </summary>
        public static string BuildAccessoryDesired(float accessoryVal)
        {
            return UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData,
                ObjectIds.AccessoryDesired,
                null,
                AccessoryDesiredData.ToPayload(accessoryVal));
        }

        /// <summary>
        /// GCSTelemetryStats-Nachricht (Handshake-Schritte und Heartbeat):
        /// 36 Null-Bytes + Status-Byte.
        /// </summary>
        public static string BuildGcsTelemetryStats(GcsTelemetryStatus status)
        {
            return UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData,
                ObjectIds.GcsTelemetryStats,
                null,
                GcsTelemetryStatsData.ToPayload(status));
        }

        /// <summary>
        /// Heartbeat-Nachricht: GCSTelemetryStats mit Status Connected (03).
        /// Muss jede Sekunde im eigenen Heartbeat-Thread gesendet werden (E4).
        /// </summary>
        public static string BuildHeartbeat()
        {
            return BuildGcsTelemetryStats(GcsTelemetryStatus.Connected);
        }
    }
}
