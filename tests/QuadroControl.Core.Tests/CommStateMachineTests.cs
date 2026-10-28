using System;
using System.Collections.Generic;
using QuadroControl.Core.Communication;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests der Kommunikations-Zustandsmaschine: zulässiger Pfad
    /// Disconnected → PortOpen → Handshaking → HeartbeatActive → Armed,
    /// unzulässige Übergänge werfen InvalidOperationException.
    /// Vor abgeschlossenem Handshake dürfen KEINE Steuersignale gesendet
    /// werden (Sicherheitsfeature d).
    /// </summary>
    public class CommStateMachineTests
    {
        private CommStateMachine Maschine()
        {
            return new CommStateMachine();
        }

        private CommStateMachine AktiverHeartbeat()
        {
            var sm = Maschine();
            sm.OpenPort();
            sm.StartHandshake();
            sm.CompleteHandshake();
            return sm;
        }

        [Fact]
        public void Startzustand_Ist_Disconnected()
        {
            Assert.Equal(CommState.Disconnected, Maschine().State);
        }

        [Fact]
        public void Zulaessiger_Pfad_Bis_Armed()
        {
            var sm = AktiverHeartbeat();
            sm.Arm();
            Assert.Equal(CommState.Armed, sm.State);
            sm.Disarm();
            Assert.Equal(CommState.HeartbeatActive, sm.State);
        }

        [Fact]
        public void Handshake_Vor_PortOffen_Wirft()
        {
            Assert.Throws<InvalidOperationException>(() => Maschine().StartHandshake());
        }

        [Fact]
        public void Arming_Vor_Handshake_Wirft()
        {
            var sm = Maschine();
            sm.OpenPort();
            sm.StartHandshake();
            Assert.Throws<InvalidOperationException>(() => sm.Arm());
        }

        [Fact]
        public void CompleteHandshake_Nur_Aus_Handshaking()
        {
            var sm = Maschine();
            sm.OpenPort();
            Assert.Throws<InvalidOperationException>(() => sm.CompleteHandshake());
        }

        [Fact]
        public void OpenPort_Zweimal_Wirft()
        {
            var sm = Maschine();
            sm.OpenPort();
            Assert.Throws<InvalidOperationException>(() => sm.OpenPort());
        }

        [Fact]
        public void Disarm_Ohne_Armed_Wirft()
        {
            Assert.Throws<InvalidOperationException>(() => AktiverHeartbeat().Disarm());
        }

        [Fact]
        public void Vor_Handshake_Keine_Steuersignale_Erlaubt()
        {
            var sm = Maschine();
            Assert.False(sm.CanSendControlCommands);
            sm.OpenPort();
            Assert.False(sm.CanSendControlCommands);
            sm.StartHandshake();
            Assert.False(sm.CanSendControlCommands);
        }

        [Fact]
        public void Nach_Handshake_Und_Armed_Steuersignale_Erlaubt()
        {
            var sm = AktiverHeartbeat();
            Assert.True(sm.CanSendControlCommands);
            sm.Arm();
            Assert.True(sm.CanSendControlCommands);
            Assert.True(sm.IsArmed);
        }

        [Fact]
        public void Close_Aus_Jedem_Zustand_Nach_Disconnected()
        {
            var sm = AktiverHeartbeat();
            sm.Close();
            Assert.Equal(CommState.Disconnected, sm.State);

            var sm2 = AktiverHeartbeat();
            sm2.Arm();
            sm2.Close();
            Assert.Equal(CommState.Disconnected, sm2.State);
        }

        [Fact]
        public void Close_Im_Startzustand_Bleibt_Disconnected()
        {
            var sm = Maschine();
            sm.Close();
            Assert.Equal(CommState.Disconnected, sm.State);
        }

        [Fact]
        public void StateChanged_Liefert_Jeden_Neuen_Zustand()
        {
            var verlaeufe = new List<CommState>();
            var sm = new CommStateMachine();
            sm.StateChanged += s => verlaeufe.Add(s);

            sm.OpenPort();
            sm.StartHandshake();
            sm.CompleteHandshake();
            sm.Arm();

            Assert.Equal(
                new[] { CommState.PortOpen, CommState.Handshaking, CommState.HeartbeatActive, CommState.Armed },
                verlaeufe);
        }
    }
}
