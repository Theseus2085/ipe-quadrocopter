namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// Datenabschnitt des UAVObjects AccessoryDesired (ObjectID C4 09 98 5A).
    /// Wirkt als "Schalter" für Arming/Disarming:
    /// AccessoryVal = +1 → Arming, AccessoryVal = -1 → Disarming (wissen §6/§7).
    /// Das Objekt ist mehrfach instanziiert, wir nutzen InstanceID 00 00
    /// (wie im VL5-Beispiel).
    /// </summary>
    public static class AccessoryDesiredData
    {
        /// <summary>AccessoryVal-Wert, der das Arming auslöst.</summary>
        public const float ArmingValue = 1f;

        /// <summary>AccessoryVal-Wert, der das Disarming auslöst.</summary>
        public const float DisarmingValue = -1f;

        /// <summary>Länge des Datenabschnitts in Bytes (ein float32).</summary>
        public const int PayloadLength = 4;

        /// <summary>
        /// Serialisiert AccessoryVal als float32 Little Endian (4 Bytes).
        /// </summary>
        public static byte[] ToPayload(float accessoryVal)
        {
            return FloatHex.ToBytes(accessoryVal);
        }
    }
}
