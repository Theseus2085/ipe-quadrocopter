using QuadroControl.Core.Protocol;

namespace QuadroControl.Core.Communication
{
    /// <summary>
    /// Kapselt die Heartbeat-Nachricht: GCSTelemetryStats mit Status 03
    /// (Connected), eine Nachricht pro Sekunde, gesendet aus einem eigenen
    /// Heartbeat-Thread (wissen §7 Schritt 3 / E4). Der Thread selbst lebt
    /// in der WPF-Anwendung — hier entsteht nur die Nachricht + Taktvorgabe.
    /// </summary>
    public static class HeartbeatMessage
    {
        /// <summary>Heartbeat-Intervall in Millisekunden (jede Sekunde, E4).</summary>
        public const int IntervalMs = 1000;

        /// <summary>
        /// Die sendefertige Heartbeat-Nachricht (Hex-String ohne Checksumme).
        /// Identisch mit der dritten Handshake-Nachricht.
        /// </summary>
        public static string Create()
        {
            return MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Connected);
        }
    }
}
