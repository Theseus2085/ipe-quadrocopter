namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// ObjectIDs der im Projekt benötigten UAVObjects.
    /// Werte aus der offiziellen Tabelle ObjectID-Uebersicht_LibrePilot_1609.xlsx,
    /// NOTATION OHNE "0x", in Tabellen-Reihenfolge (wissen-programmierung.md §5/§6):
    /// z. B. ManualControlCommand = "C4 10 74 80".
    /// ACHTUNG: In der Nachricht steht die ID Little-Endian, also byte-verdreht
    /// ("80 74 10 C4") — das übernimmt der UavTalkBuilder.
    /// </summary>
    public static class ObjectIds
    {
        /// <summary>ManualControlCommand: Steuerbefehle (Throttle/Roll/Pitch/Yaw ...).</summary>
        public static readonly byte[] ManualControlCommand = { 0xC4, 0x10, 0x74, 0x80 };

        /// <summary>AccessoryDesired: "Schalter" für Arming (1) / Disarming (-1).</summary>
        public static readonly byte[] AccessoryDesired = { 0xC4, 0x09, 0x98, 0x5A };

        /// <summary>GCSTelemetryStats: aktiviert die UAVTalk-Schnittstelle am FC (Handshake/Heartbeat).</summary>
        public static readonly byte[] GcsTelemetryStats = { 0xCA, 0xD1, 0xDC, 0x0A };
    }
}
