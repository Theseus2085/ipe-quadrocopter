using QuadroControl.Core.Protocol;

namespace QuadroControl.Core.Communication
{
    /// <summary>
    /// Die beiden Metadaten-Anpassungen des Handshakes — WORTWÖRTLICH aus
    /// VL5 Folie 39 übernommen (wissen-programmierung.md §7), byte-genau
    /// kopiert, niemals abgetippt oder neu zusammengesetzt:
    /// es sind UAVTalk-Nachrichten auf die Metadaten-Objekte
    /// (ObjectIDMeta = ObjectID + 1) von ManualControlCommand bzw.
    /// AccessoryDesired.
    /// Kleinschreibung wie in der Vorlage; die Kurs-DLL parst Hex
    /// größenunabhängig.
    /// </summary>
    public static class HandshakeMetadata
    {
        /// <summary>Metadaten-Nachricht ManualControlCommand (VL5 Folie 39, wortwörtlich).</summary>
        public const string ManualControlCommandMetadata =
            "3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00";

        /// <summary>Metadaten-Nachricht AccessoryDesired (VL5 Folie 39, wortwörtlich).</summary>
        public const string AccessoryDesiredMetadata =
            "3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00";
    }

    /// <summary>
    /// Liefert die exakte Handshake-Sequenz: GENAU 5 Nachrichten in fester
    /// Reihenfolge (wissen §7, E3):
    ///   1. GCSTelemetryStats Status 00 (Disconnected)
    ///   2. GCSTelemetryStats Status 01 (HandshakeReq)
    ///   3. GCSTelemetryStats Status 03 (Connected)
    ///   4. Metadaten ManualControlCommand (wortwörtlich aus VL5)
    ///   5. Metadaten AccessoryDesired (wortwörtlich aus VL5)
    /// Die ersten drei werden mit dem regulären UavTalkBuilder erzeugt,
    /// die letzten beiden sind unveränderliche Konstanten.
    /// </summary>
    public static class HandshakeSequenceProvider
    {
        /// <summary>Anzahl der Nachrichten des Handshakes.</summary>
        public const int MessageCount = 5;

        /// <summary>
        /// Gibt die fünf sendefertigen Hex-Strings des Handshakes zurück.
        /// </summary>
        public static string[] GetMessages()
        {
            return new[]
            {
                MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Disconnected), // 00
                MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.HandshakeReq), // 01
                MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Connected),    // 03
                HandshakeMetadata.ManualControlCommandMetadata,                         // VL5 wortwörtlich
                HandshakeMetadata.AccessoryDesiredMetadata                             // VL5 wortwörtlich
            };
        }
    }
}
