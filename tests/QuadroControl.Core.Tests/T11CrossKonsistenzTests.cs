using System;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T11 (TEST-AUFTRAG): Cross-Konsistenz — MessageFactory und UavTalkBuilder
    /// erzeugen fuer identische Eingaben identische Bytes. Um Tautologie zu
    /// vermeiden, wird der eine Pfad mit LITERALen Byte-Arrays gespeist
    /// (Objekt-ID und Payload unabhaengig hingeschrieben), nicht mit denselben
    /// Produktionskonstanten/-methoden wie der andere Pfad.
    /// </summary>
    public class T11CrossKonsistenzTests
    {
        // Unabhaengige Literale (BUILD-AUFTRAG Frozen contract / wissen §5-§7):
        private static readonly byte[] MccIdLiteral = { 0xC4, 0x10, 0x74, 0x80 };
        private static readonly byte[] AccIdLiteral = { 0xC4, 0x09, 0x98, 0x5A };
        private static readonly byte[] GcsIdLiteral = { 0xCA, 0xD1, 0xDC, 0x0A };

        private static byte[] MccPayloadLiteral()
        {
            // Throttle=0.1, Roll=0.2, Pitch=0.3, Yaw=0.4, Collective=0.25, Thrust=0.1,
            // Channel[10]=0, Connected=1, FlightModeSwitchPosition=0 — komplett von Hand:
            return Verketten(
                new byte[] { 0xCD, 0xCC, 0xCC, 0x3D }, // 0.1f
                new byte[] { 0xCD, 0xCC, 0x4C, 0x3E }, // 0.2f
                new byte[] { 0x9A, 0x99, 0x99, 0x3E }, // 0.3f
                new byte[] { 0xCD, 0xCC, 0xCC, 0x3E }, // 0.4f
                new byte[] { 0x00, 0x00, 0x80, 0x3E }, // 0.25f
                new byte[] { 0xCD, 0xCC, 0xCC, 0x3D }, // Thrust = Throttle (0.1f)
                new byte[20],                          // Channel int16[10] = 0
                new byte[] { 0x01 },                   // Connected = 1
                new byte[] { 0x00 });                  // FlightModeSwitchPosition = 0
        }

        private static byte[] Verketten(params byte[][] teile)
        {
            int laenge = 0;
            foreach (byte[] t in teile) laenge += t.Length;
            var resultat = new byte[laenge];
            int offset = 0;
            foreach (byte[] t in teile)
            {
                Array.Copy(t, 0, resultat, offset, t.Length);
                offset += t.Length;
            }
            return resultat;
        }

        private static byte[] GcsPayloadLiteral(byte status)
        {
            var payload = new byte[37]; // 36 Nullbytes ...
            payload[36] = status;       // ... + Statusbyte an letzter Position
            return payload;
        }

        // ---------- ManualControlCommand: Factory vs. Builder mit Literalen ----------

        [Fact]
        public void Mcc_FactoryPfad_Gleich_BuilderPfad_Mit_LiteralPayload()
        {
            var cmd = new ManualControlCommandData
            {
                Throttle = 0.1f,
                Roll = 0.2f,
                Pitch = 0.3f,
                Yaw = 0.4f,
                Collective = 0.25f,
                Connected = 1
            }; // Thrust wird in ToPayload auf Throttle gespiegelt -> 0.1f im Literal

            string factory = MessageFactory.BuildManualControlCommand(cmd);
            string builder = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData, MccIdLiteral, null, MccPayloadLiteral());
            Assert.Equal(builder, factory); // kein Drift zwischen den Codepfaden
        }

        [Fact]
        public void Mcc_FactoryErgebnis_Gleich_Vollstaendiger_Bekannter_Vektor()
        {
            // Zusaetzlich zum Pfadvergleich der volle erwartete String unabhaengig:
            var cmd = new ManualControlCommandData { Throttle = 0.1f, Roll = 0.2f, Pitch = 0.3f, Yaw = 0.4f, Collective = 0.25f, Connected = 1 };
            Assert.Equal(
                "3C 20 38 00 80 74 10 C4 00 00"
                + " CD CC CC 3D CD CC 4C 3E 9A 99 99 3E CD CC CC 3E 00 00 80 3E CD CC CC 3D"
                + " 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00",
                MessageFactory.BuildManualControlCommand(cmd));
        }

        // ---------- AccessoryDesired ----------

        [Theory]
        [InlineData(1f, "00 00 80 3F")]
        [InlineData(-1f, "00 00 80 BF")]
        public void Accessory_FactoryPfad_Gleich_BuilderPfad_Mit_LiteralPayload(float wert, string wertHex)
        {
            string factory = MessageFactory.BuildAccessoryDesired(wert);
            string builder = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData, AccIdLiteral, null, Hex.Parse(wertHex));
            Assert.Equal(builder, factory);
        }

        [Fact]
        public void Accessory_Disarming_Entspricht_VL5_Beispielvektor()
        {
            // wissen §5: 3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF (+ CRC der DLL)
            Assert.Equal("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF",
                MessageFactory.BuildAccessoryDesired(-1f));
            Assert.Equal("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF",
                UavTalkBuilder.BuildHex(UavTalkMessageType.ObjectData, AccIdLiteral, null,
                    new byte[] { 0x00, 0x00, 0x80, 0xBF }));
        }

        // ---------- GCSTelemetryStats ----------

        [Theory]
        [InlineData(GcsTelemetryStatus.Disconnected, (byte)0x00)]
        [InlineData(GcsTelemetryStatus.HandshakeReq, (byte)0x01)]
        [InlineData(GcsTelemetryStatus.HandshakeAck, (byte)0x02)]
        [InlineData(GcsTelemetryStatus.Connected, (byte)0x03)]
        public void Gcs_FactoryPfad_Gleich_BuilderPfad_Mit_LiteralPayload(GcsTelemetryStatus status, byte statusByte)
        {
            string factory = MessageFactory.BuildGcsTelemetryStats(status);
            string builder = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData, GcsIdLiteral, null, GcsPayloadLiteral(statusByte));
            Assert.Equal(builder, factory);
        }

        // ---------- Byte-Pfad vs. Hex-Pfad desselben Builders ----------

        [Theory]
        [InlineData("Mcc")]
        [InlineData("Accessory")]
        [InlineData("Gcs")]
        public void BuildHex_Ist_Genau_Hex_ToHexString_von_Build(string objekt)
        {
            string hexPfad;
            byte[] bytePfad;
            switch (objekt)
            {
                case "Mcc":
                    hexPfad = UavTalkBuilder.BuildHex(UavTalkMessageType.ObjectData, MccIdLiteral, null, MccPayloadLiteral());
                    bytePfad = UavTalkBuilder.Build(UavTalkMessageType.ObjectData, MccIdLiteral, null, MccPayloadLiteral());
                    break;
                case "Accessory":
                    hexPfad = UavTalkBuilder.BuildHex(UavTalkMessageType.ObjectData, AccIdLiteral, null, new byte[] { 0x00, 0x00, 0x80, 0x3F });
                    bytePfad = UavTalkBuilder.Build(UavTalkMessageType.ObjectData, AccIdLiteral, null, new byte[] { 0x00, 0x00, 0x80, 0x3F });
                    break;
                default:
                    hexPfad = UavTalkBuilder.BuildHex(UavTalkMessageType.ObjectData, GcsIdLiteral, null, GcsPayloadLiteral(0x03));
                    bytePfad = UavTalkBuilder.Build(UavTalkMessageType.ObjectData, GcsIdLiteral, null, GcsPayloadLiteral(0x03));
                    break;
            }
            Assert.Equal(hexPfad, Hex.ToHexString(bytePfad));
        }

        // ---------- Heartbeat ueber alle vier Erzeugungspfade ----------

        [Fact]
        public void Heartbeat_Ist_Ueber_Vier_Pfade_Identisch()
        {
            // E3/E4: Die Heartbeat-Nachricht ist DIE dritte Handshake-Nachricht.
            // Vier unabhaengig erreichbare Pfade duerfen nicht auseinanderlaufen:
            string direktFactory = MessageFactory.BuildHeartbeat();
            string factoryStatus = MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Connected);
            string heartbeatKlasse = HeartbeatMessage.Create();
            string dritteHandshake = HandshakeSequenceProvider.GetMessages()[2];
            Assert.Equal(direktFactory, factoryStatus);
            Assert.Equal(direktFactory, heartbeatKlasse);
            Assert.Equal(direktFactory, dritteHandshake);
        }

        // ---------- ObjectIds-Konstanten vs. Nachrichtenaufbau ----------

        [Fact]
        public void Factory_Nachrichten_Enthalten_Die_TabellenIds_Byte_Verdreht()
        {
            byte[] mcc = Hex.Parse(MessageFactory.BuildManualControlCommand(ManualControlCommandData.Neutral()));
            byte[] acc = Hex.Parse(MessageFactory.BuildAccessoryDesired(1f));
            byte[] gcs = Hex.Parse(MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Connected));

            Assert.Equal(new byte[] { 0x80, 0x74, 0x10, 0xC4 }, new[] { mcc[4], mcc[5], mcc[6], mcc[7] });
            Assert.Equal(new byte[] { 0x5A, 0x98, 0x09, 0xC4 }, new[] { acc[4], acc[5], acc[6], acc[7] });
            Assert.Equal(new byte[] { 0x0A, 0xDC, 0xD1, 0xCA }, new[] { gcs[4], gcs[5], gcs[6], gcs[7] });
        }

        [Fact]
        public void Alle_Drei_ObjektNachrichten_Haben_MessageType_ObjectData_und_Instanz_00_00()
        {
            byte[] mcc = Hex.Parse(MessageFactory.BuildManualControlCommand(ManualControlCommandData.Neutral()));
            byte[] acc = Hex.Parse(MessageFactory.BuildAccessoryDesired(-1f));
            byte[] gcs = Hex.Parse(MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.HandshakeReq));
            foreach (byte[] n in new[] { mcc, acc, gcs })
            {
                Assert.Equal(0x3C, n[0]);  // Sync
                Assert.Equal(0x20, n[1]);  // ObjectData
                Assert.Equal(0x00, n[8]);  // InstanceID
                Assert.Equal(0x00, n[9]);
            }
        }
    }
}
