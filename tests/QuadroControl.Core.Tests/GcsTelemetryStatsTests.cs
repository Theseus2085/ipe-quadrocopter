using System.Linq;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests für GCSTelemetryStats (Handshake-Schritte + Heartbeat):
    /// 37 Datenbytes = 36 Zähler-Bytes 00 + Status-Byte am Ende.
    /// </summary>
    public class GcsTelemetryStatsTests
    {
        [Fact]
        public void Payload_Hat_37_Bytes()
        {
            Assert.Equal(37, GcsTelemetryStatsData.ToPayload(GcsTelemetryStatus.Connected).Length);
        }

        [Fact]
        public void Payload_Ist_36_Mal_00_Plus_Status()
        {
            byte[] p = GcsTelemetryStatsData.ToPayload(GcsTelemetryStatus.HandshakeReq);
            Assert.All(p.Take(36), b => Assert.Equal(0, b));
            Assert.Equal(0x01, p[36]);
        }

        [Theory]
        [InlineData(GcsTelemetryStatus.Disconnected, 0x00)]
        [InlineData(GcsTelemetryStatus.HandshakeReq, 0x01)]
        [InlineData(GcsTelemetryStatus.HandshakeAck, 0x02)]
        [InlineData(GcsTelemetryStatus.Connected, 0x03)]
        public void Status_Byte_Am_Ende_Des_Payloads(GcsTelemetryStatus status, byte erwartet)
        {
            byte[] p = GcsTelemetryStatsData.ToPayload(status);
            Assert.Equal(erwartet, p[36]);
        }

        [Fact]
        public void Nachricht_Length_Feld_2F_00_ObjectID_0A_DC_D1_CA()
        {
            string hex = MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Connected);
            byte[] b = Hex.Parse(hex);

            Assert.Equal(47, b.Length);       // 10 Header + 37 Daten
            Assert.Equal(0x2F, b[2]);         // Length LE = 0x002F
            Assert.Equal(0x00, b[3]);
            Assert.Equal(0x0A, b[4]);         // Tabelle "CA D1 DC 0A" → LE "0A DC D1 CA"
            Assert.Equal(0xDC, b[5]);
            Assert.Equal(0xD1, b[6]);
            Assert.Equal(0xCA, b[7]);
        }

        [Fact]
        public void Heartbeat_Ist_GcsTelemetryStats_Status_03()
        {
            string heartbeat = HeartbeatMessage.Create();
            string erwartet = MessageFactory.BuildGcsTelemetryStats(GcsTelemetryStatus.Connected);
            Assert.Equal(erwartet, heartbeat);

            byte[] b = Hex.Parse(heartbeat);
            Assert.Equal(0x03, b[b.Length - 1]);
        }

        [Fact]
        public void Heartbeat_Intervall_Ist_Eine_Sekunde()
        {
            Assert.Equal(1000, HeartbeatMessage.IntervalMs);
        }
    }
}
