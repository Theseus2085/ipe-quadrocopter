using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SharpDX.XInput;
using vKTCQuadroControl;
using QuadroControl.Core;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Controller;
using QuadroControl.Core.Protocol;
using QuadroControl.Core.Safety;

namespace QuadroControl.Virtuell
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// Programm 1: Flug in der virtuellen Testumgebung (vKTCQuadroControl.dll).
    /// Reihenfolge im Betrieb (wissen §7): COM-Port verbinden → Handshake
    /// (5 Nachrichten) → Heartbeat jede Sekunde im eigenen Thread →
    /// Controller lesen, armen, fliegen, disarmen → trennen.
    /// </summary>
    ///

    public delegate void DelString(string hexString);

    // GUI-Update-Delegat für Zugriffe aus Nebenthreads (VL6 / E6)
    public delegate void UpdateGUIDelType(string[] values);

    public partial class MainWindow : Window
    {
        // NICHT ÄNDERN
        // ----------------------------
        private vQuadroControl vmyQC;
        DelString DelStringObject;
        // ----------------------------


        // Kommunikations-Zustandsmaschine: Disconnected → PortOpen → Handshaking
        // → HeartbeatActive → Armed. Blockiert Steuersignale/Arming, bis der
        // Handshake abgeschlossen ist (Sicherheitsfeature d, PROG-009/F3).
        private readonly CommStateMachine _commState = new CommStateMachine();

        // Transport-Adapter auf die vKTCQuadroControl-Bibliothek (PROG-007)
        private VirtuellQuadroTransport _transport;

        // Threads nach VL6: Controller-Abfrage + Heartbeat je ein eigener Thread
        private Thread ControllerThread;
        private Thread HeartbeatThread;
        private volatile bool ThreadsRunning = false;
        private volatile bool ControllerAktiv = false;

        // Xbox-Controller (SharpDX.XInput, VL6)
        private Controller XBox = new Controller(UserIndex.One);
        private State myState;

        // Sicherheitsmonitor (PROG-009A/011)
        private readonly SafetyMonitor _safety = new SafetyMonitor();

        // GUI-Update-Delegat (E6: nie direkt aus einem Thread auf die GUI)
        private UpdateGUIDelType UpdateGUIDel;

        // Totzone der Sticks: darunter gilt "kein Eingang" → neutrale Werte (PROG-011)
        private const int StickTotzone = 2500;

        public MainWindow()
        {
            // NICHT ÄNDERN
            // -------------------------------------------------
            vmyQC = new vQuadroControl();
            DelStringObject = new DelString(vSchickeHexString);
            // -------------------------------------------------

            InitializeComponent();

            // Transport kapselt die DLL; gesendet wird ausschließlich über das
            // vorgegebene, dispatcher-gesicherte vSchickeHexString (NICHT ÄNDERN).
            _transport = new VirtuellQuadroTransport(vmyQC, vSchickeHexString);
            UpdateGUIDel = UpdateGUI;
        }

        // NICHT ÄNDERN
        // ----------------------------------------------------------------------------------------------------------
        public void vSchickeHexString(String hexString)
        {
            if (!Dispatcher.CheckAccess())
            {Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, DelStringObject, hexString);}
            else {vmyQC.v_SchickeHexString(hexString); }
        }
        // ----------------------------------------------------------------------------------------------------------

        // ------------------------------------------------------------------
        // Button-Handler (Kommunikationssteuerung, PROG-008A)
        // ------------------------------------------------------------------

        /// <summary>COM-Port verbinden (virtueller Port, Name beliebig).</summary>
        private void BT_COMPortVerbinde_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string comPort = TB_COMPort.Text.Trim();
                if (comPort.Length == 0)
                {
                    ZeigeStatus("Bitte COM-Port eintragen.", null, null);
                    return;
                }

                if (_transport.Verbinde(comPort))
                {
                    _commState.OpenPort(); // Disconnected → PortOpen
                    ZeigeStatus("Verbunden mit " + comPort + " (Handshake noch ausführen).", null, null);
                }
                else
                {
                    ZeigeStatus("Verbinden mit " + comPort + " fehlgeschlagen.", null, null);
                }
            }
            catch (InvalidOperationException ex)
            {
                ZeigeStatus("Zustandsfehler: " + ex.Message, null, null);
            }
        }

        /// <summary>
        /// FC initialisieren: sendet die 5 Handshake-Nachrichten (3× GCSTelemetryStats
        /// 00/01/03 + 2 Metadaten wortwörtlich aus VL5) und startet danach den
        /// Heartbeat-Thread (E3/E4).
        /// </summary>
        private void BT_HS_HB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _commState.StartHandshake(); // PortOpen → Handshaking

                foreach (string nachricht in HandshakeSequenceProvider.GetMessages())
                {
                    _transport.SchickeHexString(nachricht);
                }

                _commState.CompleteHandshake(); // Handshaking → HeartbeatActive
                StarteThreads();
                ZeigeStatus("Handshake abgeschlossen, Heartbeat aktiv (1/s).", null, null);
            }
            catch (InvalidOperationException ex)
            {
                ZeigeStatus("Handshake nicht möglich: " + ex.Message, null, null);
            }
        }

        /// <summary>Controller-Abfrage starten/stoppen (eigener Thread, VL6).</summary>
        private void BT_XBoxRead_Click(object sender, RoutedEventArgs e)
        {
            if (ControllerThread != null && ControllerThread.IsAlive)
            {
                StoppeControllerThread();
                BT_XBoxRead.Content = "Controller lesen";
                ZeigeStatus("Controller-Abfrage gestoppt.", null, null);
            }
            else
            {
                ControllerAktiv = true;
                ThreadsRunning = true;
                ControllerThread = new Thread(ControllerThreadFunction);
                ControllerThread.IsBackground = true;
                ControllerThread.Start();
                BT_XBoxRead.Content = "Controller stoppen";
                ZeigeStatus("Controller-Abfrage läuft.", null, null);
            }
        }

        /// <summary>
        /// Verbindung trennen: Threads stoppen, ggf. entwaffnen, neutralen
        /// Steuerbefehl senden, dann trennen (wissen §7 Schritt 5).
        /// </summary>
        private void BT_COMPortTrennen_Click(object sender, RoutedEventArgs e)
        {
            VerbindungSauberTrennen();
        }

        /// <summary>Beim Schließen des Fensters ebenfalls sauber trennen (kein Motorausgang, F3).</summary>
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            VerbindungSauberTrennen();
            base.OnClosing(e);
        }

        // ------------------------------------------------------------------
        // Threads (VL6): Heartbeat + Controller
        // ------------------------------------------------------------------

        /// <summary>Heartbeat-Thread: GCSTelemetryStats Status 03, jede Sekunde (E4).</summary>
        private void HeartbeatThreadFunction()
        {
            while (ThreadsRunning)
            {
                try
                {
                    if (_commState.State == CommState.HeartbeatActive || _commState.State == CommState.Armed)
                    {
                        _transport.SchickeHexString(HeartbeatMessage.Create());
                    }
                    Thread.Sleep(QuadroConfig.HeartbeatIntervalMs);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception)
                {
                    // Verbindungsfehler: kurz warten und erneut versuchen
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>
        /// Controller-Thread: liest den Xbox-Controller, wertet die Sicherheitslogik
        /// aus und sendet ManualControlCommand — aber NUR, wenn die Zustandsmaschine
        /// es erlaubt (keine Motorwerte vor abgeschlossenem Handshake).
        /// Taste A = Arming, Taste B = Disarming (UE4).
        /// </summary>
        private void ControllerThreadFunction()
        {
            while (ThreadsRunning && ControllerAktiv)
            {
                try
                {
                    bool controllerVerbunden = XBox.IsConnected;
                    if (controllerVerbunden)
                    {
                        myState = XBox.GetState();
                    }

                    bool eingabeAktiv = controllerVerbunden && EingabeAktiv(myState);
                    SafetyDecision entscheidung =
                        _safety.Evaluate(controllerVerbunden, _commState.IsArmed, eingabeAktiv);

                    // Sicherheitsreaktion: Controller-Verlust im bewaffneten Zustand
                    // → automatisch entwaffnen (PROG-009A)
                    if (entscheidung.ShallDisarm)
                    {
                        DisarmingSequenz();
                        ZeigeStatus("Controller-Verlust erkannt: automatisch entwaffnet.", null, "Disarmed");
                    }

                    if (_commState.CanSendControlCommands)
                    {
                        // Kein Eingang → neutraler Befehl (Throttle = 0), damit die
                        // Rotoren stillstehen (PROG-011: kein "letzter Wert klebt")
                        ManualControlCommandData befehl;
                        if (entscheidung.ShallSendNeutralCommand || !controllerVerbunden)
                        {
                            befehl = SafetyMonitor.NeutralCommand();
                        }
                        else
                        {
                            befehl = ControllerMapping.ToManualControlCommand(
                                myState.Gamepad.LeftThumbX, myState.Gamepad.LeftThumbY,
                                myState.Gamepad.RightThumbX, myState.Gamepad.RightThumbY,
                                controllerVerbunden);

                            // Arming nur über Taste A im Zustand HeartbeatActive;
                            // Disarming über Taste B (E5)
                            if (TasteGedrueckt(GamepadButtonFlags.A) && !_commState.IsArmed)
                            {
                                ArmingSequenz();
                            }
                            else if (TasteGedrueckt(GamepadButtonFlags.B) && _commState.IsArmed)
                            {
                                DisarmingSequenz();
                            }
                        }
                        _transport.SchickeHexString(MessageFactory.BuildManualControlCommand(befehl));
                    }

                    ZeigeControllerWerte(controllerVerbunden);
                    Thread.Sleep(QuadroConfig.ControllerPollIntervalMs);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ZeigeStatus("Controller-Thread: " + ex.Message, null, null);
                    Thread.Sleep(500);
                }
            }
        }

        /// <summary>Startet den Heartbeat-Thread (Aufruf nach abgeschlossenem Handshake, E4).</summary>
        private void StarteThreads()
        {
            ThreadsRunning = true;
            if (HeartbeatThread == null || !HeartbeatThread.IsAlive)
            {
                HeartbeatThread = new Thread(HeartbeatThreadFunction);
                HeartbeatThread.IsBackground = true;
                HeartbeatThread.Start();
            }
        }

        /// <summary>Stoppt den Controller-Thread kontrolliert (bool-Flag statt Abort, VL6).</summary>
        private void StoppeControllerThread()
        {
            ControllerAktiv = false;
            if (ControllerThread != null && ControllerThread.IsAlive)
            {
                ControllerThread.Join(300);
            }
        }

        /// <summary>
        /// Beide Threads anhalten, entwaffnen, neutralen Befehl senden, trennen.
        /// </summary>
        private void VerbindungSauberTrennen()
        {
            try
            {
                ThreadsRunning = false;
                ControllerAktiv = false;
                if (HeartbeatThread != null && HeartbeatThread.IsAlive)
                {
                    HeartbeatThread.Join(300);
                }
                if (ControllerThread != null && ControllerThread.IsAlive)
                {
                    ControllerThread.Join(300);
                }

                if (_commState.State == CommState.HeartbeatActive || _commState.State == CommState.Armed)
                {
                    if (_commState.IsArmed)
                    {
                        DisarmingSequenz();
                    }
                    // Neutraler Befehl vor dem Trennen: Rotoren definitiv still
                    _transport.SchickeHexString(
                        MessageFactory.BuildManualControlCommand(SafetyMonitor.NeutralCommand()));
                }

                _transport.VerbindungTrennen();
                _commState.Close();
                BT_XBoxRead.Content = "Controller lesen";
                ZeigeStatus("Getrennt.", null, "Disarmed");
            }
            catch (Exception ex)
            {
                ZeigeStatus("Fehler beim Trennen: " + ex.Message, null, null);
                try { _commState.Close(); } catch (InvalidOperationException) { }
            }
        }

        // ------------------------------------------------------------------
        // Arming / Disarming (E5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Arming: ManualControlCommand mit Throttle = -1 UND Thrust = -1 mehrmals
        /// hintereinander schreiben, danach AccessoryDesired = +1 (VL5 Folie 41).
        /// </summary>
        private void ArmingSequenz()
        {
            if (_commState.State != CommState.HeartbeatActive) return; // Maschine schützt

            for (int i = 0; i < QuadroConfig.ArmingRepeatCount; i++)
            {
                _transport.SchickeHexString(
                    MessageFactory.BuildManualControlCommand(ManualControlCommandData.ForArming()));
                Thread.Sleep(QuadroConfig.ArmingRepeatDelayMs);
            }
            _transport.SchickeHexString(
                MessageFactory.BuildAccessoryDesired(AccessoryDesiredData.ArmingValue));

            try
            {
                _commState.Arm(); // HeartbeatActive → Armed
                ZeigeStatus(null, null, "Armed");
            }
            catch (InvalidOperationException)
            {
                // Zustand hat sich inzwischen geändert → nicht bewaffnen
            }
        }

        /// <summary>Disarming: AccessoryDesired = -1 schreiben (E5).</summary>
        private void DisarmingSequenz()
        {
            if (!_commState.IsArmed) return;

            _transport.SchickeHexString(
                MessageFactory.BuildAccessoryDesired(AccessoryDesiredData.DisarmingValue));
            try
            {
                _commState.Disarm(); // Armed → HeartbeatActive
                ZeigeStatus(null, null, "Disarmed");
            }
            catch (InvalidOperationException)
            {
                // bereits entwaffnet
            }
        }

        // ------------------------------------------------------------------
        // Hilfsfunktionen Controller
        // ------------------------------------------------------------------

        /// <summary>True, wenn die übergebene Taste des Controllers gedrückt ist.</summary>
        private bool TasteGedrueckt(GamepadButtonFlags taste)
        {
            return (myState.Gamepad.Buttons & taste) != 0;
        }

        /// <summary>
        /// True, wenn ein Stick über die Totzone hinaus abweicht, ein Trigger
        /// betätigt oder eine Taste gedrückt ist (Kriterium für PROG-011).
        /// </summary>
        private bool EingabeAktiv(State state)
        {
            if (Math.Abs(state.Gamepad.LeftThumbX) > StickTotzone) return true;
            if (Math.Abs(state.Gamepad.LeftThumbY) > StickTotzone) return true;
            if (Math.Abs(state.Gamepad.RightThumbX) > StickTotzone) return true;
            if (Math.Abs(state.Gamepad.RightThumbY) > StickTotzone) return true;
            if (state.Gamepad.LeftTrigger > 0 || state.Gamepad.RightTrigger > 0) return true;
            if (state.Gamepad.Buttons != 0) return true;
            return false;
        }

        // ------------------------------------------------------------------
        // GUI-Update (E6: aus Threads nur über Dispatcher + Delegat)
        // ------------------------------------------------------------------

        /// <summary>
        /// Aktualisiert die Statusanzeige aus jedem Thread. null = unverändert.
        /// Leitet über den Dispatcher um, wenn der aufrufende Thread nicht der
        /// GUI-Thread ist (System.InvalidOperationException sonst).
        /// </summary>
        private void ZeigeStatus(string verbindung, string controller, string arming)
        {
            UpdateGUI(new[] { verbindung, controller, arming, null, null, null, null, null, null });
        }

        /// <summary>Zeigt die aktuellen Controller-Werte in den TB_*-Feldern.</summary>
        private void ZeigeControllerWerte(bool controllerVerbunden)
        {
            string tasten = "-";
            if (controllerVerbunden)
            {
                var b = myState.Gamepad.Buttons;
                if ((b & GamepadButtonFlags.A) != 0) tasten = "A (Arming)";
                else if ((b & GamepadButtonFlags.B) != 0) tasten = "B (Disarming)";
                else if (b != 0) tasten = "Andere";
            }

            UpdateGUI(new[]
            {
                null, null, null,
                controllerVerbunden ? myState.Gamepad.LeftThumbX.ToString() : "-",
                controllerVerbunden ? myState.Gamepad.LeftThumbY.ToString() : "-",
                controllerVerbunden ? myState.Gamepad.RightThumbX.ToString() : "-",
                controllerVerbunden ? myState.Gamepad.RightThumbY.ToString() : "-",
                controllerVerbunden ? myState.Gamepad.LeftTrigger.ToString() : "-",
                tasten
            });
        }

        /// <summary>
        /// GUI-Aktualisierung nach VL6-Muster: Prüfung Dispatcher.CheckAccess(),
        /// sonst BeginInvoke mit dem Delegaten. values-Reihenfolge:
        /// [0] Verbindung, [1] Controller-Meldung, [2] Arming, [3] Links X,
        /// [4] Links Y, [5] Rechts X, [6] Rechts Y, [7] LeftTrigger, [8] Tasten.
        /// </summary>
        private void UpdateGUI(string[] values)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.Normal, UpdateGUIDel, values);
                return;
            }

            if (values[0] != null) Label_VerbindungStatus.Content = values[0];
            else if (values[1] != null) Label_VerbindungStatus.Content = values[1];
            if (values[2] != null) Label_ArmingStatus.Content = values[2];
            if (values[3] != null) TB_AnalogLX.Text = values[3];
            if (values[4] != null) TB_AnalogLY.Text = values[4];
            if (values[5] != null) TB_AnalogRX.Text = values[5];
            if (values[6] != null) TB_AnalogRY.Text = values[6];
            if (values[7] != null) TB_LeftTrigger.Text = values[7];
            if (values[8] != null) TB_ButtonA.Text = values[8];
        }
    }
}
