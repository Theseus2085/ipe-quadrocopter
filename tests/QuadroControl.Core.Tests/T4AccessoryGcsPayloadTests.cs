using System;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T4 (TEST-AUFTRAG): AccessoryDesired (Arming 1 / Disarming -1) und
    /// GCSTelemetryStats (36 Nullbytes + Statusbyte an letzter Position).
    /// Grundlage: wissen §6/§7, E3/E5, BUILD-AUFTRAG Frozen contract.
    /// Erwartete Bytes sind Literale, keine Referenzen auf Produktionskonstanten.
    /// </summary>
    public class T4AccessoryGcsPayloadTests
    {
        private static string Bytes(byte[] b) { return BitConverter.ToString(b).Replace("-", " "); }

        // ---------- AccessoryDesired: Konstanten ----------

        [Fact]
        public void ArmingValue_Ist_Plus1()
        {
            Assert.Equal(1f, AccessoryDesiredData.ArmingValue);
        }

        [Fact]
        public void DisarmingValue_Ist_Minus1()
        {
            Assert.Equal(-1f, AccessoryDesiredData.DisarmingValue);
        }

        [Fact]
        public void AccessoryDesired_Payload_Ist_4_Bytes()
        {
            Assert.Equal(4, AccessoryDesiredData.PayloadLength); // ein float32
        }

        // ---------- AccessoryDesired: Bytes ----------

        [Theory]
        [InlineData(1f, "00 00 80 3F")]    // Arming (E5)
        [InlineData(-1f, "00 00 80 BF")]   // Disarming (E5, VL5-Beispielbytes)
        public void AccessoryVal_Wird_Als_Float32_LittleEndian_Serialisiert(float wert, string erwartet)
        {
            Assert.Equal(erwartet, Bytes(AccessoryDesiredData.ToPayload(wert)));
        }

        [Fact]
        public void Volle_Disarming_Nachricht_Entspricht_Dem_VL5_Beispiel_Ohne_Checksumme()
        {
            // wissen §5: "3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF ??"
            // (?? = CRC der DLL). MessageFactory liefert alles vor dem ??.
            string hex = MessageFactory.BuildAccessoryDesired(-1f);
            Assert.Equal("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF", hex);
        }

        [Fact]
        public void Volle_Arming_Nachricht_Endet_Auf_00_00_80_3F()
        {
            string hex = MessageFactory.BuildAccessoryDesired(1f);
            Assert.StartsWith("3C 20 0E 00 5A 98 09 C4 00 00 ", hex);
            Assert.EndsWith("00 00 80 3F", hex);
            // 14 Bytes gesamt: Header 10 + float32 4 — kein Checksummenbyte
            Assert.Equal(14, Hex.Parse(hex).Length);
        }

        // ---------- GCSTelemetryStats: Konstanten ----------

        [Fact]
        public void Gcs_CounterBytes_Ist_36()
        {
            Assert.Equal(36, GcsTelemetryStatsData.CounterBytes);
        }

        [Fact]
        public void Gcs_PayloadLaenge_Ist_37()
        {
            // 36 Zaehler-Bytes + 1 Statusbyte (wissen §6)
            Assert.Equal(37, GcsTelemetryStatsData.PayloadLength);
        }

        // ---------- GCSTelemetryStats: Payloadstruktur ----------

        [Theory]
        [InlineData(GcsTelemetryStatus.Disconnected, 0x00)]
        [InlineData(GcsTelemetryStatus.HandshakeReq, 0x01)]
        [InlineData(GcsTelemetryStatus.HandshakeAck, 0x02)]
        [InlineData(GcsTelemetryStatus.Connected, 0x03)]
        public void Status_Liegt_An_Letzter_Position_Davor_36_Nullbytes(GcsTelemetryStatus status, byte statusByte)
        {
            byte[] payload = GcsTelemetryStatsData.ToPayload(status);
            Assert.Equal(37, payload.Length);
            for (int i = 0; i < 36; i++)
            {
                Assert.Equal(0x00, payload[i]); // TxDataRate ... RxCrcErrors alle 00
            }
            Assert.Equal(statusByte, payload[36]); // Status an LETZTER Position
        }

        [Theory]
        [InlineData(GcsTelemetryStatus.Disconnected, "00")]
        [InlineData(GcsTelemetryStatus.HandshakeReq, "01")]
        [InlineData(GcsTelemetryStatus.HandshakeAck, "02")]
        [InlineData(GcsTelemetryStatus.Connected, "03")]
        public void Gcs_Enum_Werte_Nach_wissen_Paragraph_6(GcsTelemetryStatus status, string erwartetHex)
        {
            Assert.Equal(erwartetHex, Bytes(new[] { (byte)status }));
        }

        [Theory]
        [InlineData(GcsTelemetryStatus.Disconnected)]
        [InlineData(GcsTelemetryStatus.HandshakeReq)]
        [InlineData(GcsTelemetryStatus.HandshakeAck)]
        [InlineData(GcsTelemetryStatus.Connected)]
        public void Volle_Gcs_Nachricht_Header_Byte_exakt_47_Bytes_Gesamt(GcsTelemetryStatus status)
        {
            string hex = MessageFactory.BuildGcsTelemetryStats(status);
            byte[] nachricht = Hex.Parse(hex);
            byte[] header = new byte[10];
            Array.Copy(nachricht, 0, header, 0, 10);
            // Sync 3C, Type 20, Length 2F 00 (10+37=47), ObjID LE 0A DC D1 CA, Instanz 00 00
            Assert.Equal("3C 20 2F 00 0A DC D1 CA 00 00", Bytes(header));
            Assert.Equal(47, nachricht.Length); // kein Checksummenbyte
        }

        // ---------- Handshake-Status-Reihenfolge 00 -> 01 -> 03 (E3) ----------

        [Fact]
        public void Die_Drei_Handshake_Statuse_Sind_00_Dann_01_Dann_03()
        {
            // E3: genau diese Reihenfolge; Status 02 (HandshakeAck) gehoert NICHT in den Handshake
            byte[] erste = GcsTelemetryStatsData.ToPayload(GcsTelemetryStatus.Disconnected);
            byte[] zweite = GcsTelemetryStatsData.ToPayload(GcsTelemetryStatus.HandshakeReq);
            byte[] dritte = GcsTelemetryStatsData.ToPayload(GcsTelemetryStatus.Connected);
            Assert.Equal(0x00, erste[36]);
            Assert.Equal(0x01, zweite[36]);
            Assert.Equal(0x03, dritte[36]);
        }

        [Fact]
        public void Heartbeat_Benutzt_Status_03_Genauso_Wie_Die_Dritte_Handshake_Nachricht()
        {
            // E4: Heartbeat = GCSTelemetryStats Status 03 — identische Payload
            byte[] heartbeat = GcsTelemetryStatsData.ToPayload(GcsTelemetryStatus.Connected);
            Assert.Equal(0x03, heartbeat[36]);
        }
    }
}
