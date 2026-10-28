using System;

namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// Status-Enum des UAVObjects GCSTelemetryStats (letztes Byte des
    /// Datenabschnitts, wissen §6).
    /// </summary>
    public enum GcsTelemetryStatus : byte
    {
        /// <summary>0x00: Verbindung getrennt (erste Handshake-Nachricht).</summary>
        Disconnected = 0x00,

        /// <summary>0x01: Handshake angefordert (zweite Handshake-Nachricht).</summary>
        HandshakeReq = 0x01,

        /// <summary>0x02: Handshake bestätigt.</summary>
        HandshakeAck = 0x02,

        /// <summary>0x03: verbunden (dritte Handshake-Nachricht + jede Heartbeat-Nachricht).</summary>
        Connected = 0x03
    }

    /// <summary>
    /// Datenabschnitt des UAVObjects GCSTelemetryStats (ObjectID CA D1 DC 0A).
    /// Aktiviert die UAVTalk-Schnittstelle am Flightcontroller.
    /// 37 Bytes gesamt: TxDataRate … RxCrcErrors (36 Bytes, alle 00) und am
    /// Ende der Status (1 Byte).
    /// </summary>
    public static class GcsTelemetryStatsData
    {
        /// <summary>Anzahl der Rate-/Fehler-Zähler-Felder vor dem Status-Byte (alle 00).</summary>
        public const int CounterBytes = 36;

        /// <summary>Gesamtlänge des Datenabschnitts in Bytes.</summary>
        public const int PayloadLength = CounterBytes + 1;

        /// <summary>
        /// Serialisiert den Datenabschnitt: 36 Null-Bytes + Status-Byte.
        /// </summary>
        public static byte[] ToPayload(GcsTelemetryStatus status)
        {
            byte[] payload = new byte[PayloadLength];
            payload[PayloadLength - 1] = (byte)status;
            return payload;
        }
    }
}
