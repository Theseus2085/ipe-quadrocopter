using System;

namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// Datenabschnitt des UAVObjects ManualControlCommand (ObjectID C4 10 74 80).
    /// Wichtigstes Objekt: sämtliche Steuerbefehle an den Flightcontroller.
    /// Feldreihenfolge FEST und ALLE Felder immer senden (wissen §6 / E2):
    /// Throttle, Roll, Pitch, Yaw, Collective(=0), Thrust(=Throttle),
    /// Channel int16[10](=0), Connected, FlightModeSwitchPosition(=0).
    /// Ergibt immer genau 46 Bytes: 6×float32 + 10×int16 + 2×uint8.
    /// </summary>
    public sealed class ManualControlCommandData
    {
        /// <summary>1) Throttle float32: -1 = deaktiviert (für Arming nötig), 0…1 = Gasstellung.</summary>
        public float Throttle;

        /// <summary>2) Roll float32: -1…1.</summary>
        public float Roll;

        /// <summary>3) Pitch float32: -1…1.</summary>
        public float Pitch;

        /// <summary>4) Yaw float32: -1…1.</summary>
        public float Yaw;

        /// <summary>5) Collective float32: für Flug unwichtig, immer 0.</summary>
        public float Collective;

        /// <summary>6) Thrust float32: immer gleich dem Throttle-Wert.</summary>
        public float Thrust;

        /// <summary>8) Connected (1 Byte): 0 = Disconnected, 1 = Connected (für Flug benötigt).</summary>
        public byte Connected;

        /// <summary>9) FlightModeSwitchPosition uint8: 0…5, 00 bei Lehrstuhlkonfiguration.</summary>
        public byte FlightModeSwitchPosition;

        /// <summary>Anzahl der int16-Kanäle im Channel-Array (Feld 7, 20 Bytes, immer 0).</summary>
        public const int ChannelCount = 10;

        /// <summary>Gesamtlänge des Datenabschnitts in Bytes.</summary>
        public const int PayloadLength = 46;

        /// <summary>
        /// Erzeugt eine neutrale Steuerung: alle Werte 0, Connected = 0.
        /// Wird bei fehlender Controller-Eingabe gesendet, damit die Rotoren
        /// stillstehen (PROG-011: kein "letzter Wert klebt").
        /// </summary>
        public static ManualControlCommandData Neutral()
        {
            return new ManualControlCommandData
            {
                Throttle = 0f,
                Roll = 0f,
                Pitch = 0f,
                Yaw = 0f,
                Collective = 0f,
                Thrust = 0f,
                Connected = 0,
                FlightModeSwitchPosition = 0
            };
        }

        /// <summary>
        /// Erzeugt den ManualControlCommand für die Arming-Sequenz:
        /// Throttle = -1 UND Thrust = -1 (VL5 Folie 41) — nur damit ist
        /// Arming überhaupt möglich. Rest neutrale Werte.
        /// </summary>
        public static ManualControlCommandData ForArming()
        {
            ManualControlCommandData cmd = Neutral();
            cmd.Throttle = -1f;
            cmd.Thrust = -1f; // muss Throttle entsprechen bzw. hier ebenfalls -1 sein
            return cmd;
        }

        /// <summary>
        /// Serialisiert alle 9 Felder in fester Reihenfolge nach Little Endian
        /// (46 Bytes). Channel[10] ist immer 0 und wird nicht gespeichert.
        /// </summary>
        public byte[] ToPayload()
        {
            // Thrust muss dem Throttle entsprechen (Kopplung laut wissen §6)
            Thrust = Throttle;

            byte[] payload = new byte[PayloadLength];
            int offset = 0;

            // Felder 1–6: je float32 Little Endian
            byte[] throttle = FloatHex.ToBytes(Throttle);
            Array.Copy(throttle, 0, payload, offset, 4); offset += 4;
            Array.Copy(FloatHex.ToBytes(Roll), 0, payload, offset, 4); offset += 4;
            Array.Copy(FloatHex.ToBytes(Pitch), 0, payload, offset, 4); offset += 4;
            Array.Copy(FloatHex.ToBytes(Yaw), 0, payload, offset, 4); offset += 4;
            Array.Copy(FloatHex.ToBytes(Collective), 0, payload, offset, 4); offset += 4;
            Array.Copy(FloatHex.ToBytes(Thrust), 0, payload, offset, 4); offset += 4;

            // Feld 7: Channel int16[10] — 20 Bytes, alle 00 (bleibt bei Array-Default)

            // Felder 8–9: Connected und FlightModeSwitchPosition, je 1 Byte
            payload[PayloadLength - 2] = Connected;
            payload[PayloadLength - 1] = FlightModeSwitchPosition;

            return payload;
        }
    }
}
