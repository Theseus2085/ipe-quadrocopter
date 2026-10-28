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
        /// Anzahl der Wiederholungen des ManualControlCommand mit
        /// Throttle = -1 / Thrust = -1 vor dem Arming (VL5: "mehrmals
        /// hintereinander schreiben").
        /// </summary>
        public const int ArmingRepeatCount = 5;

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
