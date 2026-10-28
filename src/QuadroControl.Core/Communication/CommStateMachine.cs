using System;

namespace QuadroControl.Core.Communication
{
    /// <summary>
    /// Zustände der Kommunikation zwischen Steuerprogramm und Flightcontroller
    /// (bzw. virtueller Testumgebung). Der Abschluss des Handshakes ist die
    /// Voraussetzung für Steuersignale und Arming — vorher werden KEINE
    /// Motorwerte gesendet (Sicherheitsfeature d, PROG-009/F3).
    /// </summary>
    public enum CommState
    {
        /// <summary>Keine Verbindung (Programmstart oder nach dem Trennen).</summary>
        Disconnected,

        /// <summary>COM-Port geöffnet (Verbinde/vVerbinde erfolgreich).</summary>
        PortOpen,

        /// <summary>Handshake läuft (5 Nachrichten werden gesendet).</summary>
        Handshaking,

        /// <summary>Handshake abgeschlossen, Heartbeat aktiv — Steuersignale erlaubt.</summary>
        HeartbeatActive,

        /// <summary>Quadrocopter ist bewaffnet (Armed) — Motoren können laufen.</summary>
        Armed
    }

    /// <summary>
    /// Zustandsmaschine für den Kommunikationsablauf (Disconnected → PortOpen →
    /// Handshaking → HeartbeatActive → Armed). Unzulässige Übergänge werfen
    /// eine InvalidOperationException, damit Programmierfehler (z. B. Arming
    /// vor abgeschlossenem Handshake) sofort auffallen und niemals still
    /// Motorwerte auslösen können.
    /// Zulässige Übergänge:
    ///   Disconnected → PortOpen          (Verbinde)
    ///   PortOpen → Handshaking           (Handshake starten)
    ///   Handshaking → HeartbeatActive    (5. Nachricht gesendet, Heartbeat läuft)
    ///   HeartbeatActive → Armed          (Arming-Sequenz abgeschlossen)
    ///   Armed → HeartbeatActive          (Disarming)
    ///   alle (außer Disconnected) → Disconnected (Trennen/Verlust)
    /// </summary>
    public sealed class CommStateMachine
    {
        /// <summary>Aktueller Zustand (nur über die Übergangsmethoden änderbar).</summary>
        public CommState State { get; private set; }

        /// <summary>Feuert bei jedem zulässigen Zustandswechsel (für GUI-Status).</summary>
        public event Action<CommState> StateChanged;

        /// <summary>Erzeugt die Maschine im sicheren Anfangszustand Disconnected.</summary>
        public CommStateMachine()
        {
            State = CommState.Disconnected;
        }

        /// <summary>True, sobald Steuersignale (ManualControlCommand) gesendet werden dürfen.</summary>
        public bool CanSendControlCommands
        {
            get { return State == CommState.HeartbeatActive || State == CommState.Armed; }
        }

        /// <summary>True, wenn der Quadrocopter bewaffnet ist.</summary>
        public bool IsArmed
        {
            get { return State == CommState.Armed; }
        }

        /// <summary>Übergang Disconnected → PortOpen nach erfolgreichem Verbinde().</summary>
        public void OpenPort()
        {
            Require(CommState.Disconnected, "Öffnen des COM-Ports nur aus Zustand Disconnected möglich.");
            Transition(CommState.PortOpen);
        }

        /// <summary>Übergang PortOpen → Handshaking beim Start der 5 Handshake-Nachrichten.</summary>
        public void StartHandshake()
        {
            Require(CommState.PortOpen, "Handshake nur nach geöffnetem COM-Port möglich.");
            Transition(CommState.Handshaking);
        }

        /// <summary>Übergang Handshaking → HeartbeatActive nach der 5. Nachricht.</summary>
        public void CompleteHandshake()
        {
            Require(CommState.Handshaking, "Abschluss des Handshakes nur aus Zustand Handshaking möglich.");
            Transition(CommState.HeartbeatActive);
        }

        /// <summary>Übergang HeartbeatActive → Armed nach abgeschlossener Arming-Sequenz.</summary>
        public void Arm()
        {
            Require(CommState.HeartbeatActive, "Arming nur mit aktivem Heartbeat (nach abgeschlossenem Handshake) möglich.");
            Transition(CommState.Armed);
        }

        /// <summary>Übergang Armed → HeartbeatActive nach dem Disarming.</summary>
        public void Disarm()
        {
            Require(CommState.Armed, "Disarming nur im Zustand Armed möglich.");
            Transition(CommState.HeartbeatActive);
        }

        /// <summary>
        /// Übergang in Disconnected aus jedem Zustand (Trennen der Verbindung,
        /// Verbindungsverlust, Programmende). Ein bewaffneter QC wird zuvor
        /// von der Anwendung entwaffnet (siehe SafetyMonitor).
        /// </summary>
        public void Close()
        {
            if (State == CommState.Disconnected) return;
            Transition(CommState.Disconnected);
        }

        private void Require(CommState expected, string message)
        {
            if (State != expected)
            {
                throw new InvalidOperationException(
                    message + " Aktuell: " + State + ", erwartet: " + expected + ".");
            }
        }

        private void Transition(CommState newState)
        {
            State = newState;
            var handler = StateChanged;
            if (handler != null) handler(newState);
        }
    }
}
