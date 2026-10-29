using System;
using QuadroControl.Core.Protocol;
using QuadroControl.Core.Safety;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T7 (TEST-AUFTRAG): SafetyMonitor — Controller-Verlusterkennung (PROG-009A)
    /// und Neutralverhalten (PROG-011) als VOLLE Entscheidungsmatrix, plus
    /// Verlust -> Wiederherstellung -> erneuter Verlust und die Inhalte der
    /// SafetyDecision-Objekte.
    /// </summary>
    public class T7SafetyMonitorDecisionTests
    {
        // ---------- Volle Matrix: (verbunden, bewaffnet, Eingabe aktiv) ----------

        [Theory]
        // Controller getrennt: Verlust immer erkannt, neutrale Werte immer erzwungen
        [InlineData(false, false, false, true, false, true)]
        [InlineData(false, false, true, true, false, true)]
        [InlineData(false, true, false, true, true, true)]
        [InlineData(false, true, true, true, true, true)]
        // Controller verbunden, aber KEINE Eingabe: neutral senden (PROG-011)
        [InlineData(true, false, false, false, false, true)]
        [InlineData(true, true, false, false, false, true)]
        // Controller verbunden UND Eingabe aktiv: nichts zwangsweise senden
        [InlineData(true, false, true, false, false, false)]
        [InlineData(true, true, true, false, false, false)]
        public void Volle_Entscheidungsmatrix(
            bool controllerConnected, bool isArmed, bool anyInputActive,
            bool erwartetLost, bool erwartetDisarm, bool erwartetNeutral)
        {
            var monitor = new SafetyMonitor();
            SafetyDecision d = monitor.Evaluate(controllerConnected, isArmed, anyInputActive);
            Assert.Equal(erwartetLost, d.ControllerLost);
            Assert.Equal(erwartetDisarm, d.ShallDisarm);
            Assert.Equal(erwartetNeutral, d.ShallSendNeutralCommand);
        }

        // ---------- Controller-Verlust pro Zustand (PROG-009A) ----------

        [Theory]
        [InlineData(false)] // nicht bewaffnet: kein Disarming noetig, aber Stillstand
        [InlineData(true)]  // bewaffnet: Disarming MUSS ausgeloest werden
        public void Verlust_Erzwingt_Neutralcommand_Immer_Disarming_Nur_Wenn_Bewaffnet(bool isArmed)
        {
            var monitor = new SafetyMonitor();
            SafetyDecision d = monitor.Evaluate(false, isArmed, true);
            Assert.True(d.ControllerLost);
            Assert.True(d.ShallSendNeutralCommand); // Motoren stillstellen in JEDEM Fall
            Assert.Equal(isArmed, d.ShallDisarm);
        }

        [Fact]
        public void Verlust_Ueberschreibt_Aktive_Eingabe()
        {
            // Selbst wenn der Bediener gerade Sticks betaetigt: ohne Controller
            // duerfen KEINE Steuerwerte mehr rausgehen.
            var monitor = new SafetyMonitor();
            SafetyDecision d = monitor.Evaluate(false, true, true);
            Assert.True(d.ShallSendNeutralCommand);
            Assert.True(d.ShallDisarm);
        }

        // ---------- Neutralverhalten (PROG-011) ----------

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Keine_Eingabe_Am_Verbundenen_Controller_Fuehrt_Zum_Neutralcommand(bool isArmed)
        {
            var monitor = new SafetyMonitor();
            SafetyDecision d = monitor.Evaluate(true, isArmed, false);
            Assert.False(d.ControllerLost);
            Assert.False(d.ShallDisarm); // kein Grund zu disarmieren, Controller ist da
            Assert.True(d.ShallSendNeutralCommand); // "letzter Wert klebt" ist verboten
        }

        [Fact]
        public void Aktive_Eingabe_Verbunden_Laeuft_Normal_Kein_Zwang()
        {
            var monitor = new SafetyMonitor();
            SafetyDecision d = monitor.Evaluate(true, false, true);
            Assert.False(d.ControllerLost);
            Assert.False(d.ShallDisarm);
            Assert.False(d.ShallSendNeutralCommand);
        }

        // ---------- Verlust -> Wiederherstellung -> erneuter Verlust ----------

        [Fact]
        public void Verlust_Wiederherstellung_Erneuter_Verlust()
        {
            var monitor = new SafetyMonitor();

            // 1) Verlust waehrend des Flugs: Disarming + Neutral
            SafetyDecision verlust1 = monitor.Evaluate(false, true, false);
            Assert.True(verlust1.ControllerLost);
            Assert.True(verlust1.ShallDisarm);

            // Anwendung hat entwaffnet (Zustandsmaschine zurueck auf HeartbeatActive)
            SafetyDecision verlust1Danach = monitor.Evaluate(false, false, false);
            Assert.True(verlust1Danach.ControllerLost);
            Assert.False(verlust1Danach.ShallDisarm); // nicht doppelt disarmieren
            Assert.True(verlust1Danach.ShallSendNeutralCommand);

            // 2) Controller kehrt zurueck, Bediener greift wieder ein
            SafetyDecision wiederDa = monitor.Evaluate(true, false, true);
            Assert.False(wiederDa.ControllerLost);
            Assert.False(wiederDa.ShallSendNeutralCommand);

            // 3) Erneuter Verlust (z. B. Akku leer) — wieder vollstaendige Reaktion
            SafetyDecision verlust2 = monitor.Evaluate(false, false, true);
            Assert.True(verlust2.ControllerLost);
            Assert.True(verlust2.ShallSendNeutralCommand);
            Assert.False(verlust2.ShallDisarm); // ist nicht (mehr) bewaffnet

            // 4) Erneut bewaffnet und wieder verloren: Disarming erneut gefordert
            SafetyDecision verlust3 = monitor.Evaluate(false, true, false);
            Assert.True(verlust3.ShallDisarm);
        }

        [Fact]
        public void Monitor_Ist_Zustandslos_Gleiche_Eingaben_Gleiche_Entscheidung()
        {
            var m1 = new SafetyMonitor();
            var m2 = new SafetyMonitor();
            for (int verbunden = 0; verbunden <= 1; verbunden++)
            {
                for (int bewaffnet = 0; bewaffnet <= 1; bewaffnet++)
                {
                    for (int eingabe = 0; eingabe <= 1; eingabe++)
                    {
                        SafetyDecision a = m1.Evaluate(verbunden == 1, bewaffnet == 1, eingabe == 1);
                        SafetyDecision b = m2.Evaluate(verbunden == 1, bewaffnet == 1, eingabe == 1);
                        Assert.Equal(a.ControllerLost, b.ControllerLost);
                        Assert.Equal(a.ShallDisarm, b.ShallDisarm);
                        Assert.Equal(a.ShallSendNeutralCommand, b.ShallSendNeutralCommand);
                    }
                }
            }
        }

        // ---------- NeutralCommand-Inhalte ----------

        [Fact]
        public void NeutralCommand_Alle_Werte_Null_Throttle_0()
        {
            ManualControlCommandData n = SafetyMonitor.NeutralCommand();
            Assert.Equal(0f, n.Throttle); // PROG-011: Stillstand der Rotoren
            Assert.Equal(0f, n.Roll);
            Assert.Equal(0f, n.Pitch);
            Assert.Equal(0f, n.Yaw);
            Assert.Equal(0f, n.Collective);
            Assert.Equal(0f, n.Thrust);
            Assert.Equal(0, n.Connected);
            Assert.Equal(0, n.FlightModeSwitchPosition);
        }

        [Fact]
        public void NeutralCommand_Payload_Ist_Komplett_Null()
        {
            byte[] payload = SafetyMonitor.NeutralCommand().ToPayload();
            Assert.Equal(46, payload.Length);
            for (int i = 0; i < payload.Length; i++)
            {
                Assert.Equal(0x00, payload[i]);
            }
        }

        // ---------- SafetyDecision-Objekt ----------

        [Fact]
        public void SafetyDecision_Standardwert_Alles_Falsch()
        {
            var d = new SafetyDecision();
            Assert.False(d.ControllerLost);
            Assert.False(d.ShallDisarm);
            Assert.False(d.ShallSendNeutralCommand);
        }

        [Fact]
        public void Evaluate_Liefert_Jedes_Mal_Ein_Neues_Decision_Objekt()
        {
            var monitor = new SafetyMonitor();
            SafetyDecision a = monitor.Evaluate(true, false, true);
            SafetyDecision b = monitor.Evaluate(true, false, true);
            Assert.NotSame(a, b); // kein geteiltes, mutierbares Objekt zwischen Zyklen
        }
    }
}
