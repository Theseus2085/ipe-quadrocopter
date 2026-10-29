using System;
using System.Collections.Generic;
using QuadroControl.Core.Communication;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T6 (TEST-AUFTRAG): CommStateMachine — VOLLE Transitionsmatrix:
    /// jeder Zustand x jedes Ereignis, legal und illegal. Illegal =
    /// InvalidOperationException (dokumentiert im Klassenkommentar des Codes:
    /// "Unzulaessige Uebergaenge werfen eine InvalidOperationException").
    /// Zulaessig laut Vertrag:
    ///   Disconnected->PortOpen, PortOpen->Handshaking, Handshaking->HeartbeatActive,
    ///   HeartbeatActive->Armed, Armed->HeartbeatActive (Disarming),
    ///   alle ausser Disconnected -> Disconnected (Close), Close aus Disconnected = No-Op.
    /// </summary>
    public class T6CommStateMachineMatrixTests
    {
        public enum Ereignis { OpenPort, StartHandshake, CompleteHandshake, Arm, Disarm, Close }

        /// <summary>Zustaand gezielt herstellen (nur ueber legale Uebergaenge).</summary>
        private static CommStateMachine MaschineIn(CommState zustand)
        {
            var m = new CommStateMachine();
            if (zustand == CommState.Disconnected) return m;
            m.OpenPort();
            if (zustand == CommState.PortOpen) return m;
            m.StartHandshake();
            if (zustand == CommState.Handshaking) return m;
            m.CompleteHandshake();
            if (zustand == CommState.HeartbeatActive) return m;
            m.Arm();
            Assert.Equal(CommState.Armed, m.State);
            return m;
        }

        private static void Ausloesen(CommStateMachine m, Ereignis e)
        {
            switch (e)
            {
                case Ereignis.OpenPort: m.OpenPort(); break;
                case Ereignis.StartHandshake: m.StartHandshake(); break;
                case Ereignis.CompleteHandshake: m.CompleteHandshake(); break;
                case Ereignis.Arm: m.Arm(); break;
                case Ereignis.Disarm: m.Disarm(); break;
                case Ereignis.Close: m.Close(); break;
            }
        }

        // Legale Zustaende der Matrix: 5 Zustaende x 6 Ereignisse = 30 Zellen,
        // hier aufgeteilt in die 6 legalen und die 24 illegalen Uebergaenge.

        [Theory]
        [InlineData(CommState.Disconnected, Ereignis.OpenPort, CommState.PortOpen)]
        [InlineData(CommState.Disconnected, Ereignis.Close, CommState.Disconnected)] // No-Op, keine Exception
        [InlineData(CommState.PortOpen, Ereignis.StartHandshake, CommState.Handshaking)]
        [InlineData(CommState.PortOpen, Ereignis.Close, CommState.Disconnected)]
        [InlineData(CommState.Handshaking, Ereignis.CompleteHandshake, CommState.HeartbeatActive)]
        [InlineData(CommState.Handshaking, Ereignis.Close, CommState.Disconnected)]
        [InlineData(CommState.HeartbeatActive, Ereignis.Arm, CommState.Armed)]
        [InlineData(CommState.HeartbeatActive, Ereignis.Close, CommState.Disconnected)]
        [InlineData(CommState.Armed, Ereignis.Disarm, CommState.HeartbeatActive)]
        [InlineData(CommState.Armed, Ereignis.Close, CommState.Disconnected)]
        public void Matrix_Legale_Uebergaenge_Fuehren_In_Den_Folgezustand(CommState ausgang, Ereignis ereignis, CommState erwartet)
        {
            CommStateMachine m = MaschineIn(ausgang);
            Assert.Equal(ausgang, m.State); // Herstellung hat geklappt

            Ausloesen(m, ereignis);
            Assert.Equal(erwartet, m.State);
        }

        [Theory]
        // Disconnected
        [InlineData(CommState.Disconnected, Ereignis.StartHandshake)]
        [InlineData(CommState.Disconnected, Ereignis.CompleteHandshake)]
        [InlineData(CommState.Disconnected, Ereignis.Arm)]
        [InlineData(CommState.Disconnected, Ereignis.Disarm)]
        // PortOpen
        [InlineData(CommState.PortOpen, Ereignis.OpenPort)]
        [InlineData(CommState.PortOpen, Ereignis.CompleteHandshake)]
        [InlineData(CommState.PortOpen, Ereignis.Arm)]
        [InlineData(CommState.PortOpen, Ereignis.Disarm)]
        // Handshaking
        [InlineData(CommState.Handshaking, Ereignis.OpenPort)]
        [InlineData(CommState.Handshaking, Ereignis.StartHandshake)]
        [InlineData(CommState.Handshaking, Ereignis.Arm)] // Arming vor Handshake = Sicherheitsverstoß
        [InlineData(CommState.Handshaking, Ereignis.Disarm)]
        // HeartbeatActive
        [InlineData(CommState.HeartbeatActive, Ereignis.OpenPort)]
        [InlineData(CommState.HeartbeatActive, Ereignis.StartHandshake)]
        [InlineData(CommState.HeartbeatActive, Ereignis.CompleteHandshake)]
        [InlineData(CommState.HeartbeatActive, Ereignis.Disarm)] // Disarm nur aus Armed
        // Armed
        [InlineData(CommState.Armed, Ereignis.OpenPort)]
        [InlineData(CommState.Armed, Ereignis.StartHandshake)]
        [InlineData(CommState.Armed, Ereignis.CompleteHandshake)]
        [InlineData(CommState.Armed, Ereignis.Arm)] // doppelt bewaffnen nicht moeglich
        public void Matrix_Illegale_Uebergaenge_Werfen_InvalidOperationException(CommState ausgang, Ereignis ereignis)
        {
            CommStateMachine m = MaschineIn(ausgang);
            Assert.Equal(ausgang, m.State);

            Assert.Throws<InvalidOperationException>(() => Ausloesen(m, ereignis));
            Assert.Equal(ausgang, m.State); // Zustand unveraendert nach Abweisung
        }

        // ---------- Disarm aus jedem Zustand explizit (Sicherheitsthema) ----------

        [Theory]
        [InlineData(CommState.Disconnected)]
        [InlineData(CommState.PortOpen)]
        [InlineData(CommState.Handshaking)]
        [InlineData(CommState.HeartbeatActive)]
        public void Disarm_Aus_Nicht_Armed_Wirft_In_Jedem_Fall(CommState zustand)
        {
            CommStateMachine m = MaschineIn(zustand);
            Assert.Throws<InvalidOperationException>(() => m.Disarm());
            Assert.Equal(zustand, m.State);
        }

        [Fact]
        public void Disarm_Aus_Armed_Fuehrt_Zurueck_Nach_HeartbeatActive()
        {
            CommStateMachine m = MaschineIn(CommState.Armed);
            m.Disarm();
            Assert.Equal(CommState.HeartbeatActive, m.State);
        }

        // ---------- Vollstaendiger Lebenszyklus ----------

        [Fact]
        public void Kompletter_Lebenszyklus_Disconnected_bis_Armed_bis_Disconnected()
        {
            var m = new CommStateMachine();
            Assert.Equal(CommState.Disconnected, m.State);

            m.OpenPort();
            Assert.Equal(CommState.PortOpen, m.State);
            m.StartHandshake();
            Assert.Equal(CommState.Handshaking, m.State);
            m.CompleteHandshake();
            Assert.Equal(CommState.HeartbeatActive, m.State);
            m.Arm();
            Assert.Equal(CommState.Armed, m.State);
            m.Disarm();
            Assert.Equal(CommState.HeartbeatActive, m.State);
            m.Close();
            Assert.Equal(CommState.Disconnected, m.State);
        }

        [Fact]
        public void Wiederverbindung_Nach_Trennung_Ist_Moeglich()
        {
            // Verbindung -> kompletter Ablauf -> Trennen -> ERNEUT verbinden + bewaffnen
            var m = MaschineIn(CommState.Armed);
            m.Close();
            Assert.Equal(CommState.Disconnected, m.State);

            m.OpenPort();
            m.StartHandshake();
            m.CompleteHandshake();
            m.Arm();
            Assert.Equal(CommState.Armed, m.State); // zweite Runde vollstaendig
        }

        [Fact]
        public void Close_Aus_Jedem_Nicht_Disconnected_Zustand_Fuehrt_Nach_Disconnected()
        {
            foreach (CommState zustand in new[]
            {
                CommState.PortOpen, CommState.Handshaking, CommState.HeartbeatActive, CommState.Armed
            })
            {
                CommStateMachine m = MaschineIn(zustand);
                m.Close();
                Assert.Equal(CommState.Disconnected, m.State);
            }
        }

        [Fact]
        public void Close_Aus_Disconnected_Ist_NoOp_Ohne_Exception()
        {
            var m = new CommStateMachine();
            m.Close();
            m.Close(); // zweimal gefahrlos
            Assert.Equal(CommState.Disconnected, m.State);
        }

        // ---------- StateChanged-Ereignis ----------

        [Fact]
        public void StateChanged_Feuert_Fuer_Jeden_Legalen_Uebergang_Mit_Dem_Folgezustand()
        {
            var m = new CommStateMachine();
            var verlauf = new List<CommState>();
            m.StateChanged += verlauf.Add;

            m.OpenPort();
            m.StartHandshake();
            m.CompleteHandshake();
            m.Arm();
            m.Disarm();
            m.Close();

            Assert.Equal(new[]
            {
                CommState.PortOpen, CommState.Handshaking, CommState.HeartbeatActive,
                CommState.Armed, CommState.HeartbeatActive, CommState.Disconnected
            }, verlauf);
        }

        [Fact]
        public void StateChanged_Feuert_Nicht_Bei_Illegalem_Uebergang_Und_Nicht_Bei_CloseAusDisconnected()
        {
            var m = new CommStateMachine();
            int feuert = 0;
            m.StateChanged += s => feuert++;

            Assert.Throws<InvalidOperationException>(() => m.Arm()); // illegal
            Assert.Equal(0, feuert);
            m.Close(); // No-Op aus Disconnected
            Assert.Equal(0, feuert);
        }

        // ---------- Guard-Eigenschaften (Sicherheit) ----------

        [Fact]
        public void Startzustand_Ist_Disconnected()
        {
            Assert.Equal(CommState.Disconnected, new CommStateMachine().State);
        }

        [Theory]
        [InlineData(CommState.Disconnected, false)]
        [InlineData(CommState.PortOpen, false)]
        [InlineData(CommState.Handshaking, false)]
        [InlineData(CommState.HeartbeatActive, true)]
        [InlineData(CommState.Armed, true)]
        public void CanSendControlCommands_Nur_Nach_Abgeschlossenem_Handshake(CommState zustand, bool erwartet)
        {
            // Sicherheitsfeature (d): vor HeartbeatActive KEINE Motorwerte
            CommStateMachine m = MaschineIn(zustand);
            Assert.Equal(erwartet, m.CanSendControlCommands);
        }

        [Theory]
        [InlineData(CommState.Disconnected, false)]
        [InlineData(CommState.PortOpen, false)]
        [InlineData(CommState.Handshaking, false)]
        [InlineData(CommState.HeartbeatActive, false)]
        [InlineData(CommState.Armed, true)]
        public void IsArmed_Nur_Im_Zustand_Armed(CommState zustand, bool erwartet)
        {
            CommStateMachine m = MaschineIn(zustand);
            Assert.Equal(erwartet, m.IsArmed);
        }

        [Fact]
        public void Ausnahmemeldung_Nennt_Aktuellen_Und_Erwarteten_Zustand()
        {
            var m = new CommStateMachine(); // Disconnected, Arm ist illegal
            var ex = Assert.Throws<InvalidOperationException>(() => m.Arm());
            Assert.Contains("Disconnected", ex.Message);
            Assert.Contains("HeartbeatActive", ex.Message);
        }
    }
}
