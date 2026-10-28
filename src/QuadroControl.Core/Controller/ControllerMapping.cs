using QuadroControl.Core.Protocol;

namespace QuadroControl.Core.Controller
{
    /// <summary>
    /// Belegung des Xbox-360-Controllers, Mode-2 (Entscheidung D3, dokumentiert
    /// in BUILD-AUFTRAG/controllerbelegung):
    ///   linker Stick Y  = Throttle (roh -32768…32767 → 0…1)
    ///   linker Stick X  = Yaw      (skaliert auf -1…1)
    ///   rechter Stick X = Roll     (skaliert auf -1…1)
    ///   rechter Stick Y = Pitch    (skaliert auf -1…1)
    ///   Taste A = Arming, Taste B = Disarming (nach UE4)
    /// SharpDX.XInput liefert die Achsen als short (-32768…32767).
    /// </summary>
    public static class ControllerMapping
    {
        /// <summary>Größter positiver Achsenwert von XInput (für die -1…1-Skalierung).</summary>
        public const short AxisMax = 32767;

        /// <summary>Kleinster negativer Achsenwert von XInput (für die 0…1-Skalierung des Gas-Sticks).</summary>
        public const short AxisMin = -32768;

        /// <summary>
        /// Linker Stick Y → Throttle 0…1 (Mode-2: Stick oben = mehr Gas).
        /// Abbildung (-32768…32767) → (0…1), auf [0;1] begrenzt.
        /// </summary>
        public static float ThrottleFromAxis(short leftStickY)
        {
            float value = (leftStickY - AxisMin) / (float)(AxisMax - AxisMin);
            return Clamp(value, 0f, 1f);
        }

        /// <summary>Linker Stick X → Yaw -1…1.</summary>
        public static float YawFromAxis(short leftStickX)
        {
            return AxisToNormalized(leftStickX);
        }

        /// <summary>Rechter Stick X → Roll -1…1.</summary>
        public static float RollFromAxis(short rightStickX)
        {
            return AxisToNormalized(rightStickX);
        }

        /// <summary>Rechter Stick Y → Pitch -1…1.</summary>
        public static float PitchFromAxis(short rightStickY)
        {
            return AxisToNormalized(rightStickY);
        }

        /// <summary>
        /// Skaliert eine XInput-Achse (-32767…32767) linear auf -1…1
        /// und begrenzt den Wert auf den Zielbereich.
        /// </summary>
        public static float AxisToNormalized(short axisValue)
        {
            return Clamp(axisValue / (float)AxisMax, -1f, 1f);
        }

        /// <summary>
        /// Baut aus den vier Achsen einen vollständigen ManualControlCommand
        /// (alle Felder belegt, Collective=0, Thrust=Throttle, Connected=1,
        /// FlightModeSwitchPosition=0).
        /// </summary>
        public static ManualControlCommandData ToManualControlCommand(
            short leftStickX, short leftStickY, short rightStickX, short rightStickY, bool controllerConnected)
        {
            return new ManualControlCommandData
            {
                Throttle = ThrottleFromAxis(leftStickY),
                Roll = RollFromAxis(rightStickX),
                Pitch = PitchFromAxis(rightStickY),
                Yaw = YawFromAxis(leftStickX),
                Collective = 0f,
                Thrust = ThrottleFromAxis(leftStickY), // wird in ToPayload() erneut mit Throttle synchronisiert
                Connected = controllerConnected ? (byte)1 : (byte)0,
                FlightModeSwitchPosition = 0
            };
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
