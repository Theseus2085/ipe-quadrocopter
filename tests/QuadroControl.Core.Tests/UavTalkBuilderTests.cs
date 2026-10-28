using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests des UAVTalk-Rahmens (Sync, Type, Length Little Endian, ObjectID
    /// byte-verdreht, InstanceID). Eingefrorener Known-Vektor aus BUILD-AUFTRAG
    /// bzw. VL5-Beispiel (AccessoryDesired, ohne Checksumme):
    ///   3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF
    /// Hinweis: Dieser Vektor trägt den Wert -1.0 (= Disarming-Beispiel der
    /// VL5-Folie); derselbe Rahmen mit +1.0 wird als Arming-Variante geprüft.
    /// </summary>
    public class UavTalkBuilderTests
    {
        [Fact]
        public void AccessoryDesired_Negativ_Liefert_Eingefrorenen_VL5_Vektor()
        {
            string hex = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData, ObjectIds.AccessoryDesired, null,
                AccessoryDesiredData.ToPayload(-1.0f));

            Assert.Equal("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF", hex);
        }

        [Fact]
        public void AccessoryDesired_Plus1_Liefert_Gleichen_Rahmen_Mit_80_3F()
        {
            string hex = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData, ObjectIds.AccessoryDesired, null,
                AccessoryDesiredData.ToPayload(1.0f));

            Assert.Equal("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 3F", hex);
        }

        [Fact]
        public void AccessoryDesired_Nachricht_Hat_Keine_Checksumme()
        {
            // 14 Bytes = 10 Header + 4 Daten, KEIN 15. Byte (CRC hängt die DLL an)
            byte[] bytes = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.AccessoryDesired, null,
                AccessoryDesiredData.ToPayload(1.0f));

            Assert.Equal(14, bytes.Length);
            Assert.Equal(14, (bytes[3] << 8) | bytes[2]); // Length-Feld LE = 0x000E
        }

        [Fact]
        public void Sync_Byte_Ist_3C_Und_Type_Ist_20()
        {
            byte[] bytes = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand, null,
                new byte[46]);

            Assert.Equal(0x3C, bytes[0]);
            Assert.Equal(0x20, bytes[1]);
        }

        [Fact]
        public void Length_Feld_Ist_LittleEndian_Ohne_Checksumme()
        {
            // ManualControlCommand: 46 Datenbytes → Length = 56 = 0x0038 → LE "38 00"
            byte[] mcc = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand, null, new byte[46]);
            Assert.Equal(0x38, mcc[2]);
            Assert.Equal(0x00, mcc[3]);

            // GCSTelemetryStats: 37 Datenbytes → Length = 47 = 0x002F → LE "2F 00"
            byte[] gts = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.GcsTelemetryStats, null, new byte[37]);
            Assert.Equal(0x2F, gts[2]);
            Assert.Equal(0x00, gts[3]);
        }

        [Fact]
        public void ObjectIDs_Stehen_Byte_Verdreht_In_Der_Nachricht()
        {
            // Tabelle: ManualControlCommand "C4 10 74 80" → Nachricht "80 74 10 C4"
            byte[] mcc = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand, null, new byte[0]);
            Assert.Equal(0x80, mcc[4]);
            Assert.Equal(0x74, mcc[5]);
            Assert.Equal(0x10, mcc[6]);
            Assert.Equal(0xC4, mcc[7]);
        }

        [Fact]
        public void InstanceID_Standardmaessig_00_00()
        {
            byte[] bytes = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.AccessoryDesired, null, new byte[0]);
            Assert.Equal(0x00, bytes[8]);
            Assert.Equal(0x00, bytes[9]);
        }

        [Fact]
        public void BuildHex_Liefert_Grossbuchstaben_Mit_Leerzeichen()
        {
            string hex = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData, ObjectIds.AccessoryDesired, null, new byte[0]);
            Assert.Matches("^([0-9A-F]{2} )*[0-9A-F]{2}$", hex);
        }
    }
}
