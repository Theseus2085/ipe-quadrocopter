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
        /// Live-Analyse der vKTCQuadroControl.dll (03.11.2026, dekompiliert):
        /// die DLL legt einen 50 Einträge tiefen ThrottleList-Puffer an und
        /// schaltet erst bei Summe -50 (50 konsekutive MCCs mit Throttle -1)
        /// von Arming_Vorgemerkt auf Arming_Aktiv; jeder neutrale MCC vorher
        /// bricht ab. Deshalb 55 (50 + Reserve), AccessoryDesired geht mittig.
        /// </summary>
        public const int ArmingRepeatCount = 55;

        /// <summary>
        /// Position der AccessoryDesired-Nachricht im Arming-Burst (0-basiert).
        /// Die DLL verlangt: letzter MCC vor AccessoryDesired hat Throttle -1
        /// und der Burst läuft danach weiter, bis 50 konsekutive -1 erreicht sind.
        /// </summary>
        public const int ArmingAccessoryIndex = 4;

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
