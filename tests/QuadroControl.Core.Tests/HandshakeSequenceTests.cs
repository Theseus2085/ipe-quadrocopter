using System.Linq;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests der exakten Handshake-Sequenz: GENAU 5 Nachrichten —
    /// 3× GCSTelemetryStats (Status 00, 01, 03) und danach die ZWEI
    /// Metadaten-Nachrichten WORTWÖRTLICH aus VL5 Folie 39
    /// (wissen-programmierung.md §7). Die Vergleichs-Strings stehen hier
    /// absichtlich NOCHMAL unabhängig im Test, byte-für-byte aus der
    /// Wissensdatei übernommen.
    /// </summary>
    public class HandshakeSequenceTests
    {
        // Wortwörtlich aus wissen-programmierung.md §7 (VL5 Folie 39):
        private const string Vl5MetadataManualControlCommand =
            "3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00";
        private const string Vl5MetadataAccessoryDesired =
            "3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00";

        [Fact]
        public void Handshake_Besteht_Aus_Genau_5_Nachrichten()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            Assert.Equal(5, messages.Length);
        }

        [Fact]
        public void Erste_Drei_Nachrichten_GcsTelemetryStats_00_01_03()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            byte[] statusBytes = messages.Take(3).Select(m => Hex.Parse(m).Last()).ToArray();

            Assert.Equal(new byte[] { 0x00, 0x01, 0x03 }, statusBytes);
        }

        [Fact]
        public void Erste_Drei_Nachrichten_Trage_Die_GcsTelemetryStats_ObjectID()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            foreach (string m in messages.Take(3))
            {
                byte[] b = Hex.Parse(m);
                Assert.Equal(0x0A, b[4]);
                Assert.Equal(0xDC, b[5]);
                Assert.Equal(0xD1, b[6]);
                Assert.Equal(0xCA, b[7]);
            }
        }

        [Fact]
        public void Vierte_Nachricht_ManualControlCommand_Metadaten_Wortwörtlich()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            Assert.Equal(Vl5MetadataManualControlCommand, messages[3]);
        }

        [Fact]
        public void Fuenfte_Nachricht_AccessoryDesired_Metadaten_Wortwörtlich()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            Assert.Equal(Vl5MetadataAccessoryDesired, messages[4]);
        }

        [Fact]
        public void Metadaten_Konstanten_Sind_Byte_Identisch_Zu_VL5()
        {
            Assert.Equal(Vl5MetadataManualControlCommand, HandshakeMetadata.ManualControlCommandMetadata);
            Assert.Equal(Vl5MetadataAccessoryDesired, HandshakeMetadata.AccessoryDesiredMetadata);
        }

        [Fact]
        public void Metadaten_Sind_Gueltige_UavTalk_Rahmen_18_Bytes()
        {
            byte[] mccMeta = Hex.Parse(HandshakeMetadata.ManualControlCommandMetadata);
            byte[] accMeta = Hex.Parse(HandshakeMetadata.AccessoryDesiredMetadata);

            Assert.Equal(18, mccMeta.Length); // 0x12 = Header 10 + Daten 8
            Assert.Equal(18, accMeta.Length);
            Assert.All(new[] { mccMeta, accMeta }, m => Assert.Equal(0x3C, m[0]));
            Assert.All(new[] { mccMeta, accMeta }, m => Assert.Equal(0x20, m[1]));
            Assert.All(new[] { mccMeta, accMeta }, m => Assert.Equal(0x12, m[2]));
        }

        [Fact]
        public void Metadaten_ObjectID_Ist_ObjectID_Plus_1()
        {
            // ManualControlCommand C4 10 74 80 → Meta C4 10 74 81 (in der Nachricht LE "81 74 10 c4")
            byte[] mccMeta = Hex.Parse(HandshakeMetadata.ManualControlCommandMetadata);
            Assert.Equal(new byte[] { 0x81, 0x74, 0x10, 0xC4 }, mccMeta.Skip(4).Take(4).ToArray());
        }
    }
}
