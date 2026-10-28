using QuadroControl.Core.Protocol;

namespace QuadroControl.Core.Safety
{
    /// <summary>
    /// Entscheidung des Sicherheitsmonitors für einen Abfragezyklus des
    /// Controller-Threads. Die Reihenfolge der Aktionen in der Anwendung:
    /// erst ggf. entwaffnen (Disarming), dann neutrale Steuerung senden.
    /// </summary>
    public sealed class SafetyDecision
    {
        /// <summary>True, wenn die Disarming-Sequenz ausgelöst werden muss (Controller-Verlust im bewaffneten Zustand).</summary>
        public bool ShallDisarm;

        /// <summary>True, wenn ein neutraler ManualControlCommand (Throttle=0) zu senden ist.</summary>
        public bool ShallSendNeutralCommand;

        /// <summary>True, wenn der Controller-Verlust erkannt wurde (für die Statusanzeige).</summary>
        public bool ControllerLost;
    }

    /// <summary>
    /// Sicherheitslogik (PROG-009/009A/011), bewertet rein funktional — ohne
    /// Threads und ohne GUI, damit sie unit-testbar ist. Die WPF-Anwendung
    /// ruft Evaluate() in jedem Durchlauf des Controller-Threads auf:
    ///   (a) Erkennung des Verbindungsverlusts des Controllers (XBox.IsConnected
    ///       pollen) → automatisches Disarming + Statuswechsel,
    ///   (b) immer dann, wenn kein Controller-Eingang vorliegt, neutraler
    ///       ManualControlCommand (Throttle = 0), damit die Rotoren
    ///       stillstehen — kein "letzter Wert klebt".
    /// </summary>
    public sealed class SafetyMonitor
    {
        /// <summary>
        /// Bewertet die aktuelle Lage. Parameter:
        /// </summary>
        /// <param name="controllerConnected">Ergebnis der Abfrage von XBox.IsConnected.</param>
        /// <param name="isArmed">Aktueller Arming-Zustand (Zustandsmaschine).</param>
        /// <param name="anyInputActive">True, wenn der Bediener Sticks/Tasten benutzt (Stick-Ausschläge oder Tastendruck).</param>
        public SafetyDecision Evaluate(bool controllerConnected, bool isArmed, bool anyInputActive)
        {
            var decision = new SafetyDecision();

            if (!controllerConnected)
            {
                // (a) Controller-Verlust: Disarming einleiten (falls bewaffnet)
                //     und neutrale Werte erzwingen — die Motoren müssen stillstehen.
                decision.ControllerLost = true;
                decision.ShallDisarm = isArmed;
                decision.ShallSendNeutralCommand = true;
                return decision;
            }

            if (!anyInputActive)
            {
                // (b) Kein Eingang am Controller: neutrale Steuerung senden (PROG-011).
                decision.ShallSendNeutralCommand = true;
            }

            return decision;
        }

        /// <summary>
        /// Der neutrale ManualControlCommand für den Stillstand der Rotoren:
        /// alle Werte 0 (Throttle = 0, Connected = 0).
        /// </summary>
        public static ManualControlCommandData NeutralCommand()
        {
            return ManualControlCommandData.Neutral();
        }
    }
}
