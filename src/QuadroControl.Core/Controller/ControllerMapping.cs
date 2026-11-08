using QuadroControl.Core.Protocol;

namespace QuadroControl.Core.Controller
{
    /// <summary>
    /// Belegung des Xbox-360-Controllers (Entscheidung D3v2, dokumentiert
    /// in BUILD-AUFTRAG/controllerbelegung):
    ///   R2 (rechter Trigger) = Throttle (roh 0…255 → 0…1)
    ///   linker Stick X  = Yaw      (skaliert auf -1…1)
    ///   rechter Stick X = Roll     (skaliert auf -1…1)
    ///   rechter Stick Y = Pitch    (skaliert auf -1…1)
    ///   Taste A = Arming, Taste B = Disarming (laut UE4 vorgeschrieben)
    /// Der linke Stick Y ist nicht belegt (Rohwert wird nur angezeigt).
    /// SharpDX.XInput: Achsen als short (-32768…32767), Trigger als byte (0…255).
    /// </summary>
    public static class ControllerMapping
    {
        /// <summary>Größter positiver Achsenwert von XInput (für die -1…1-Skalierung).</summary>
        public const short AxisMax = 32767;

        /// <summary>Größter Triggerwert von XInput (voller Zug = 255).</summary>
        public const byte TriggerMax = 255;

        /// <summary>
        /// Totzone des rechten Triggers: Werte bis hierhin gelten als
        /// "nicht betätigt" und ergeben 0 Gas (Ruhe-Rauschen des Triggers).
        /// </summary>
        public const byte TriggerTotzone = 10;

        /// <summary>
        /// R2 (rechter Trigger) → Throttle 0…1 (mehr Zug = mehr Gas).
        /// Abbildung (Totzone…255) → (0…1), auf [0;1] begrenzt; Werte
        /// bis zur Totzone ergeben exakt 0.
        /// </summary>
        public static float ThrottleFromTrigger(byte rightTrigger)
        {
            if (rightTrigger <= TriggerTotzone)
            {
                return 0f;
            }

            float value = (rightTrigger - TriggerTotzone) / (float)(TriggerMax - TriggerTotzone);
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
        /// Baut aus den Eingaben einen vollständigen ManualControlCommand
        /// (alle Felder belegt, Collective=0, Thrust=Throttle, Connected=1,
        /// FlightModeSwitchPosition=0). Gas kommt vom rechten Trigger (R2),
        /// der linke Stick Y geht nicht ein (nicht belegt, D3v2).
        /// </summary>
        public static ManualControlCommandData ToManualControlCommand(
            short leftStickX, short rightStickX, short rightStickY, byte rightTrigger, bool controllerConnected)
        {
            return new ManualControlCommandData
            {
                Throttle = ThrottleFromTrigger(rightTrigger),
                Roll = RollFromAxis(rightStickX),
                Pitch = PitchFromAxis(rightStickY),
                Yaw = YawFromAxis(leftStickX),
                Collective = 0f,
                Thrust = ThrottleFromTrigger(rightTrigger), // wird in ToPayload() erneut mit Throttle synchronisiert
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
