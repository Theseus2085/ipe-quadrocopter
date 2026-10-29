using System;
using System.Collections.Generic;
using System.Text;
using QuadroControl.Core;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T10 (TEST-AUFTRAG): Golden Mission Stream — die vollstaendige Byte-Sequenz
    /// einer simulierten Mission (verbinden -> Handshake -> 3 Heartbeats -> Arming
    /// -> Steuerkommandos -> Disarming -> Neutralkommandos), JEDE Nachricht einzeln
    /// asserted. Die Sequenz wird ZWEIMAL erzeugt und muss byte-identisch sein.
    /// Die Mission nutzt ausschliesslich die oeffentlichen Core-APIs (so wie es
    /// die WPF-Apps tun); der Transport ist ein aufzeichnendes Fake.
    /// </summary>
    public class T10GoldenMissionStreamTests
    {
        private const string MccMetaWissen = "3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00";
        private const string AccMetaWissen = "3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00";
        private const string DisarmingVektor = "3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF";
        private const string ArmingVektor = "3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 3F";

        /// <summary>Zeichnet alles Gesendete auf (IQuadroTransport-Fake, DLL-frei).</summary>
        private sealed class AufzeichnenderTransport : IQuadroTransport
        {
            public readonly List<string> Gesendet = new List<string>();
            public bool Verbinde(string comPort) { return true; }
            public bool VerbindungTrennen() { return true; }
            public void SchickeHexString(string hexString) { Gesendet.Add(hexString); }
        }

        private sealed class MissionErgebnis
        {
            public List<string> Nachrichten;
            public List<CommState> Zustaende;
        }

        /// <summary>
        /// Komplette Mission ueber die oeffentlichen Core-APIs. Reihenfolge nach
        /// wissen §7 (5 Schritte) und E4/E5. Nachrichten-Indizes:
        /// 0-4 Handshake, 5-7 Heartbeats, 8-12 MCC Throttle=-1 (5x laut Config),
        /// 13 AccessoryDesired=1, 14-16 Steuerkommandos, 17 AccessoryDesired=-1,
        /// 18-19 Neutralkommandos.
        /// </summary>
        private static MissionErgebnis MissionErzeugen()
        {
            var transport = new AufzeichnenderTransport();
            var sm = new CommStateMachine();
            var zustaende = new List<CommState>();
            sm.StateChanged += zustaende.Add;

            // Schritt 1: Verbindung
            sm.OpenPort();

            // Schritt 2: Handshake (5 Nachrichten)
            sm.StartHandshake();
            foreach (string nachricht in HandshakeSequenceProvider.GetMessages())
            {
                transport.SchickeHexString(nachricht);
            }
            sm.CompleteHandshake();

            // Schritt 3: Heartbeat 3x (E4: jede Sekunde — hier nur 3 Takte simuliert)
            for (int i = 0; i < 3; i++)
            {
                transport.SchickeHexString(HeartbeatMessage.Create());
            }

            // Schritt 4a: Arming (E5: MCC mit Throttle=-1 UND Thrust=-1 mehrfach, dann Accessory=1)
            for (int i = 0; i < QuadroConfig.ArmingRepeatCount; i++)
            {
                transport.SchickeHexString(
                    MessageFactory.BuildManualControlCommand(ManualControlCommandData.ForArming()));
            }
            transport.SchickeHexString(MessageFactory.BuildAccessoryDesired(QuadroConfig.AccessoryArmingValue));
            sm.Arm();

            // Schritt 4b: Steuerkommandos (Muster Roll/Pitch/Yaw/Throttle)
            transport.SchickeHexString(MessageFactory.BuildManualControlCommand(
                Controller.ControllerMapping.ToManualControlCommand(0, 16384, 32767, -32767, true)));
            transport.SchickeHexString(MessageFactory.BuildManualControlCommand(
                Controller.ControllerMapping.ToManualControlCommand(-32767, 32767, -32767, 32767, true)));
            transport.SchickeHexString(MessageFactory.BuildManualControlCommand(
                Controller.ControllerMapping.ToManualControlCommand(8192, 0, 0, 0, true)));

            // Schritt 4c: Disarming (AccessoryDesired = -1)
            transport.SchickeHexString(MessageFactory.BuildAccessoryDesired(QuadroConfig.AccessoryDisarmingValue));
            sm.Disarm();

            // Neutralkommandos nach dem Disarming (PROG-011: Rotoren still)
            transport.SchickeHexString(MessageFactory.BuildManualControlCommand(Safety.SafetyMonitor.NeutralCommand()));
            transport.SchickeHexString(MessageFactory.BuildManualControlCommand(ManualControlCommandData.Neutral()));

            // Schritt 5: Trennen
            sm.Close();

            return new MissionErgebnis { Nachrichten = transport.Gesendet, Zustaende = zustaende };
        }

        private static byte[] Parse(string hex) { return Hex.Parse(hex); }

        private static float FloatAn(byte[] nachricht, int payloadOffset)
        {
            byte[] feld = new byte[4];
            Array.Copy(nachricht, 10 + payloadOffset, feld, 0, 4);
            return FloatHex.FromBytes(feld);
        }

        // ---------- Gesamtform ----------

        [Fact]
        public void Mission_Umfasst_Exakt_20_Nachrichten()
        {
            MissionErgebnis mission = MissionErzeugen();
            Assert.Equal(20, mission.Nachrichten.Count);
        }

        [Fact]
        public void Mission_Durchlaeuft_Den_Zustandsverlauf_Der_CommStateMachine()
        {
            MissionErgebnis mission = MissionErzeugen();
            Assert.Equal(new[]
            {
                CommState.PortOpen, CommState.Handshaking, CommState.HeartbeatActive,
                CommState.Armed, CommState.HeartbeatActive, CommState.Disconnected
            }, mission.Zustaende);
        }

        // ---------- Handshake (Indizes 0-4), jede Nachricht einzeln ----------

        [Theory]
        [InlineData(0, 0x00)]
        [InlineData(1, 0x01)]
        [InlineData(2, 0x03)]
        public void Nachricht_0_bis_2_GcsTelemetryStats_Status_00_01_03(int index, byte status)
        {
            byte[] n = Parse(MissionErzeugen().Nachrichten[index]);
            Assert.Equal(0x3C, n[0]);
            Assert.Equal(0x20, n[1]);
            Assert.Equal(47, n.Length);
            Assert.Equal(status, n[46]);
        }

        [Fact]
        public void Nachricht_3_Mcc_Metadaten_Wortwuertlich_Aus_wissen_Paragraph_7()
        {
            Assert.Equal(MccMetaWissen, MissionErzeugen().Nachrichten[3]);
        }

        [Fact]
        public void Nachricht_4_AccessoryDesired_Metadaten_Wortwuertlich_Aus_wissen_Paragraph_7()
        {
            Assert.Equal(AccMetaWissen, MissionErzeugen().Nachrichten[4]);
        }

        // ---------- Heartbeats (Indizes 5-7) ----------

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void Nachrichten_5_bis_7_Sind_Je_Ein_Heartbeat_Status_03(int index)
        {
            byte[] n = Parse(MissionErzeugen().Nachrichten[index]);
            Assert.Equal(0x3C, n[0]);
            Assert.Equal(0x20, n[1]);
            Assert.Equal("3C 20 2F 00 0A DC D1 CA 00 00", Erste10(n));
            Assert.Equal(0x03, n[46]);
        }

        [Fact]
        public void Alle_Drei_Heartbeats_Sind_Byte_Identisch_Zur_Dritten_Handshake_Nachricht()
        {
            MissionErgebnis mission = MissionErzeugen();
            for (int i = 5; i <= 7; i++)
            {
                Assert.Equal(mission.Nachrichten[2], mission.Nachrichten[i]);
            }
        }

        // ---------- Arming (Indizes 8-12 + 13) ----------

        [Theory]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        public void Arming_Mcc_Throttle_Minus1_UND_Thrust_Minus1_Anzahl_Wie_Config(int index)
        {
            byte[] n = Parse(MissionErzeugen().Nachrichten[index]);
            Assert.Equal("3C 20 38 00 80 74 10 C4 00 00", Erste10(n)); // MCC-Header, Length 56
            Assert.Equal(-1f, FloatAn(n, 0));   // Throttle = -1
            Assert.Equal(-1f, FloatAn(n, 20));  // Thrust = -1 (E5: BEIDE -1)
            Assert.Equal(0f, FloatAn(n, 4));    // Roll neutral
            Assert.Equal(0f, FloatAn(n, 8));    // Pitch neutral
            Assert.Equal(0f, FloatAn(n, 12));   // Yaw neutral
            Assert.Equal(0, n[54]);             // Connected = 0
            Assert.Equal(0, n[55]);             // FlightModeSwitchPosition = 0
        }

        [Fact]
        public void Arming_Mcc_Wird_Exakt_ArmingRepeatCount_Mal_Gesendet()
        {
            MissionErgebnis mission = MissionErzeugen();
            string erstes = mission.Nachrichten[8];
            for (int i = 8; i < 8 + QuadroConfig.ArmingRepeatCount; i++)
            {
                Assert.Equal(erstes, mission.Nachrichten[i]); // alle identisch, keine Drift
            }
        }

        [Fact]
        public void Nachricht_13_Ist_AccessoryDesired_Plus1_Nach_Den_Mcc_Wiederholungen()
        {
            Assert.Equal(ArmingVektor, MissionErzeugen().Nachrichten[13]);
        }

        // ---------- Steuerkommandos (Indizes 14-16) ----------

        [Fact]
        public void Nachricht_14_Steuerkommando_Muster_Roll_Pitch_Yaw_Throttle()
        {
            // rechtsX=32767 -> Roll=+1, rechtsY=-32767 -> Pitch=-1, linksX=0 -> Yaw~0,
            // linksY=16384 -> Throttle ~0.75
            byte[] n = Parse(MissionErzeugen().Nachrichten[14]);
            Assert.Equal("3C 20 38 00 80 74 10 C4 00 00", Erste10(n));
            Assert.InRange(FloatAn(n, 0), 0.7499f, 0.7501f); // Throttle ~0.75
            Assert.Equal(1f, FloatAn(n, 4));                 // Roll = +1
            Assert.Equal(-1f, FloatAn(n, 8));                // Pitch = -1
            Assert.Equal(0f, FloatAn(n, 12));                // Yaw = 0 (linksX neutral)
            Assert.Equal(1, n[54]);                          // Connected = 1 waehrend des Flugs
        }

        [Fact]
        public void Nachricht_15_Gegenlaengiges_Steuerkommando()
        {
            // linksX=-32767 -> Yaw=-1, linksY=32767 -> Throttle=1,
            // rechtsX=-32767 -> Roll=-1, rechtsY=32767 -> Pitch=+1
            byte[] n = Parse(MissionErzeugen().Nachrichten[15]);
            Assert.Equal(1f, FloatAn(n, 0));   // Throttle
            Assert.Equal(-1f, FloatAn(n, 4));  // Roll
            Assert.Equal(1f, FloatAn(n, 8));   // Pitch
            Assert.Equal(-1f, FloatAn(n, 12)); // Yaw
        }

        [Fact]
        public void Nachricht_16_Nur_Yaw_Ausschlag_Rest_Neutral()
        {
            byte[] n = Parse(MissionErzeugen().Nachrichten[16]);
            // linksX=8192 -> Yaw = 8192/32767 ~ 0.25001
            Assert.InRange(FloatAn(n, 12), 0.2499f, 0.2501f);
            // linksY=0 -> Throttle = 32768/65535 ~ 0.5000076 (Mittelstellung ~ halbes Gas)
            Assert.InRange(FloatAn(n, 0), 0.4999f, 0.5001f);
            Assert.Equal(0f, FloatAn(n, 4));
            Assert.Equal(0f, FloatAn(n, 8));
            // Thrust spiegelt Throttle auch im laufenden Steuerkommando
            Assert.Equal(FloatAn(n, 0), FloatAn(n, 20));
        }

        // ---------- Disarming + Neutral (Indizes 17-19) ----------

        [Fact]
        public void Nachricht_17_Ist_AccessoryDesired_Minus1()
        {
            Assert.Equal(DisarmingVektor, MissionErzeugen().Nachrichten[17]);
        }

        [Theory]
        [InlineData(18)]
        [InlineData(19)]
        public void Nachrichten_18_19_Sind_Neutrale_Mcc_mit_46_Nullbytes(int index)
        {
            byte[] n = Parse(MissionErzeugen().Nachrichten[index]);
            Assert.Equal("3C 20 38 00 80 74 10 C4 00 00", Erste10(n));
            for (int i = 10; i < 56; i++)
            {
                Assert.Equal(0x00, n[i]); // alle Werte 0: Stillstand der Rotoren (PROG-011)
            }
        }

        // ---------- Determinismus: zweimal erzeugen, byte-identisch ----------

        [Fact]
        public void Mission_Zweimal_Erzeugt_Liefert_Byte_Identische_Streams()
        {
            List<string> erste = MissionErzeugen().Nachrichten;
            List<string> zweite = MissionErzeugen().Nachrichten;
            Assert.Equal(erste.Count, zweite.Count);
            for (int i = 0; i < erste.Count; i++)
            {
                Assert.True(Hex.SameBytes(erste[i], zweite[i]),
                    "Nachricht " + i + " unterscheidet sich zwischen beiden Laeufen");
                Assert.Equal(erste[i], zweite[i]); // auch als String identisch
            }
        }

        [Fact]
        public void Keine_Einzige_Missionsnachricht_Enthaelt_Eine_Checksumme()
        {
            // E1: CRC haengt ausschliesslich die Kurs-DLL an. Fuer ALLE 20 Nachrichten
            // muss gelten: Length-Feld == tatsaechliche Bytezahl.
            foreach (string hex in MissionErzeugen().Nachrichten)
            {
                byte[] n = Parse(hex);
                int lengthFeld = n[2] | (n[3] << 8);
                Assert.Equal(lengthFeld, n.Length);
            }
        }

        private static string Erste10(byte[] nachricht)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(nachricht[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}
