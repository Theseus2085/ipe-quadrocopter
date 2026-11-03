namespace QuadroControl.Core
{
    /// <summary>
    /// Zentrale Konfigurationswerte der Steuerungsapplikation ("config from
    /// Core"). Bewusst als einfache Konstanten gehalten, damit beide
    /// WPF-Programme (Testumgebung und realer QC) identische Takte und
    /// Sequenzlängen verwenden.
    /// </summary>
    public static class QuadroConfig
    {
        /// <summary>Heartbeat-Intervall in Millisekunden (1 Nachricht pro Sekunde, E4).</summary>
        public const int HeartbeatIntervalMs = 1000;

        /// <summary>
        /// Anzahl der MCC-Nachrichten mit Throttle = -1 / Thrust = -1 im
        /// Arming-Burst (VL5: "mehrmals hintereinander schreiben").
        /// Live-Analyse der vKTCQuadroControl.dll (03.11.2026, dekompiliert +
        /// Zustands-Dump per Reflexion): die DLL puffert die letzten 50 MCC-
        /// Throttle-Werte und setzt Arming nur vorübergehend (Vorgemerkt), wenn
        /// AccessoryDesired=1 eintrifft; der nächstfolgende MCC entscheidet:
        /// ThrottleList-Summe -50 (50 konsekutive -1) -> Arming_Aktiv, sonst
        /// Abbruch. Deshalb: 55x MCC(-1), DANN AccessoryDesired, DANN nochmal
        /// MCC(-1) als abschließende Nachricht (siehe ArmingSequenz).
        /// </summary>
        public const int ArmingRepeatCount = 55;

        /// <summary>Pause zwischen den Arming-Wiederholungen in Millisekunden.</summary>
        public const int ArmingRepeatDelayMs = 50;

        /// <summary>Takt des Controller-Threads in Millisekunden (Abfrage + Senden).</summary>
        public const int ControllerPollIntervalMs = 20;

        /// <summary>AccessoryVal für Arming (+1) — siehe AccessoryDesiredData.</summary>
        public const float AccessoryArmingValue = 1f;

        /// <summary>AccessoryVal für Disarming (-1) — siehe AccessoryDesiredData.</summary>
        public const float AccessoryDisarmingValue = -1f;
    }
}
