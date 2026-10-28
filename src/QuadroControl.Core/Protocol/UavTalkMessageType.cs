namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// UAVTalk Message-Types (Header-Feld B, wissen-programmierung.md §5).
    /// Der Steuerapp sendet ausschließlich Objektdatenübertragung (0x20).
    /// </summary>
    public enum UavTalkMessageType : byte
    {
        /// <summary>Objektdatenübertragung (0x20) — Datentransfer PC → FC.</summary>
        ObjectData = 0x20,

        /// <summary>Objektanfrage (0x21).</summary>
        ObjectRequest = 0x21,

        /// <summary>Übertragung mit Bestätigungsanfrage (0x22).</summary>
        ObjectAcknowledged = 0x22,

        /// <summary>Positive Bestätigung (0x23).</summary>
        Ack = 0x23,

        /// <summary>Negative Bestätigung (0x24).</summary>
        Nak = 0x24
    }
}
