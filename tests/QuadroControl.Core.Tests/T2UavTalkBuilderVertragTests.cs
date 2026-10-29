using System;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T2 (TEST-AUFTRAG): UavTalkBuilder + ObjectIds — Vertragspruefung nach
    /// wissen-programmierung.md §5 (Felder A-F) und BUILD-AUFTRAG "Frozen contract".
    /// Alle erwarteten Bytes sind Literale aus den Grundlagendateien, nie
    /// Referenzen auf ObjectIds-* (keine Tautologie).
    /// </summary>
    public class T2UavTalkBuilderVertragTests
    {
        private static string Bytes(byte[] b) { return BitConverter.ToString(b).Replace("-", " "); }

        // ---------- Sync-Byte (Feld A) und Header-Laenge ----------

        [Fact]
        public void SyncByte_Ist_3C_Nach_Protokoll()
        {
            Assert.Equal(0x3C, UavTalkBuilder.SyncByte);
        }

        [Fact]
        public void HeaderLaenge_Ist_10_Bytes()
        {
            // A(1) + B(1) + C(2) + D(4) + E(2) = 10
            Assert.Equal(10, UavTalkBuilder.HeaderLength);
        }

        [Theory]
        [InlineData(0x20)] // ObjectData
        [InlineData(0x21)] // ObjectRequest
        [InlineData(0x22)] // ObjectAcknowledged
        [InlineData(0x23)] // Ack
        [InlineData(0x24)] // Nak
        public void Jeder_MessageType_Landet_In_Byte_1_mit_Sync_3C_davor(byte type)
        {
            byte[] nachricht = UavTalkBuilder.Build(
                (UavTalkMessageType)type, new byte[] { 0xC4, 0x10, 0x74, 0x80 }, null, new byte[0]);
            Assert.Equal(0x3C, nachricht[0]); // Sync unabhaengig vom Type
            Assert.Equal(type, nachricht[1]);
        }

        [Theory]
        [InlineData(UavTalkMessageType.ObjectData, 0x20)]
        [InlineData(UavTalkMessageType.ObjectRequest, 0x21)]
        [InlineData(UavTalkMessageType.ObjectAcknowledged, 0x22)]
        [InlineData(UavTalkMessageType.Ack, 0x23)]
        [InlineData(UavTalkMessageType.Nak, 0x24)]
        public void MessageType_Enum_Werte_Nach_wissen_Paragraph_5(UavTalkMessageType type, byte erwartet)
        {
            Assert.Equal(erwartet, (byte)type);
        }

        // ---------- Length-Feld (Feld C): int16 LE, Header+Daten ohne Checksumme ----------

        [Theory]
        [InlineData(46, "38 00")] // ManualControlCommand-Payload: 10+46=56=0x0038
        [InlineData(4, "0E 00")]  // AccessoryDesired-Payload: 10+4=14=0x000E (VL5-Beispiel!)
        [InlineData(37, "2F 00")] // GCSTelemetryStats-Payload: 10+37=47=0x002F
        [InlineData(0, "0A 00")]  // leere Daten
        [InlineData(300, "36 01")] // 10+300=310=0x0136 -> Pruefung Uebertrag ins 2. Byte
        public void Length_Feld_Ist_Header_Plus_Daten_LittleEndian_ohne_Checksumme(int datenLaenge, string erwartetLengthHex)
        {
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, new byte[] { 0xC4, 0x10, 0x74, 0x80 }, null, new byte[datenLaenge]);
            Assert.Equal(erwartetLengthHex, Bytes(new byte[] { nachricht[2], nachricht[3] }));
            // Gesamtlaenge == Length-Feld-Wert == exakt 10+X Bytes -> kein Checksummen-Byte angehaengt
            Assert.Equal(10 + datenLaenge, nachricht.Length);
            Assert.Equal(10 + datenLaenge, nachricht[2] | (nachricht[3] << 8));
        }

        [Fact]
        public void Length_Feld_Fuer_Grosse_Nachricht_Noch_Korrekt_LittleEndian()
        {
            // 10 + 32757 = 32767 = short.MaxValue = 0x7FFF -> "FF 7F" (zulaessige Grenze)
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, new byte[] { 0xC4, 0x10, 0x74, 0x80 }, null, new byte[32757]);
            Assert.Equal("FF 7F", Bytes(new byte[] { nachricht[2], nachricht[3] }));
            Assert.Equal(32767, nachricht.Length);
        }

        // ---------- ObjectIDs byte-exakt (Feld D, LE = Tabellenreihenfolge verdreht) ----------

        [Fact]
        public void ObjectIds_ManualControlCommand_Ist_C4_10_74_80()
        {
            Assert.Equal(new byte[] { 0xC4, 0x10, 0x74, 0x80 }, ObjectIds.ManualControlCommand);
        }

        [Fact]
        public void ObjectIds_AccessoryDesired_Ist_C4_09_98_5A()
        {
            Assert.Equal(new byte[] { 0xC4, 0x09, 0x98, 0x5A }, ObjectIds.AccessoryDesired);
        }

        [Fact]
        public void ObjectIds_GcsTelemetryStats_Ist_CA_D1_DC_0A()
        {
            Assert.Equal(new byte[] { 0xCA, 0xD1, 0xDC, 0x0A }, ObjectIds.GcsTelemetryStats);
        }

        [Theory]
        [InlineData(new byte[] { 0xC4, 0x10, 0x74, 0x80 }, "80 74 10 C4")] // MCC, wissen §5 Beispiel
        [InlineData(new byte[] { 0xC4, 0x09, 0x98, 0x5A }, "5A 98 09 C4")] // AccessoryDesired
        [InlineData(new byte[] { 0xCA, 0xD1, 0xDC, 0x0A }, "0A DC D1 CA")] // GCSTelemetryStats
        public void ObjectID_Stehen_In_Nachricht_Byte_Verdreht(byte[] objectID, string erwartetImRahmen)
        {
            byte[] nachricht = UavTalkBuilder.Build(UavTalkMessageType.ObjectData, objectID, null, new byte[0]);
            Assert.Equal(erwartetImRahmen, Bytes(new byte[] { nachricht[4], nachricht[5], nachricht[6], nachricht[7] }));
        }

        // ---------- InstanceID (Feld E) ----------

        [Fact]
        public void InstanceID_Null_Standardmaessig_00_00()
        {
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand, null, new byte[0]);
            Assert.Equal("00 00", Bytes(new byte[] { nachricht[8], nachricht[9] }));
        }

        [Fact]
        public void InstanceID_LeeresArray_Ebenfalls_00_00()
        {
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand, new byte[0], new byte[0]);
            Assert.Equal("00 00", Bytes(new byte[] { nachricht[8], nachricht[9] }));
        }

        [Fact]
        public void InstanceID_Wird_LittleEndian_Eingebaut()
        {
            // Tabellenreihenfolge {34 12} -> im Rahmen 12 34 (dokumentiertes Verhalten)
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand,
                new byte[] { 0x34, 0x12 }, new byte[0]);
            Assert.Equal("12 34", Bytes(new byte[] { nachricht[8], nachricht[9] }));
        }

        // ---------- Checksumme wird NICHT angehaengt (Feld G, DLL-Pflicht E1) ----------

        [Fact]
        public void VL5_Beispiel_AccessoryDesired_Minus1_Byte_exakt_Ohne_Checksumme()
        {
            // wissen §5: "3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF ??"
            // das "??" haengt die DLL an — der Builder liefert exakt 14 Bytes ohne es.
            string hex = UavTalkBuilder.BuildHex(
                UavTalkMessageType.ObjectData,
                new byte[] { 0xC4, 0x09, 0x98, 0x5A },
                null,
                new byte[] { 0x00, 0x00, 0x80, 0xBF });
            Assert.Equal("3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF", hex);
        }

        [Fact]
        public void Datenabschnitt_Folgt_Direkt_Nach_Byte_9()
        {
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.AccessoryDesired, null,
                new byte[] { 0x00, 0x00, 0x80, 0xBF });
            Assert.Equal(0x00, nachricht[10]);
            Assert.Equal(0x00, nachricht[11]);
            Assert.Equal(0x80, nachricht[12]);
            Assert.Equal(0xBF, nachricht[13]);
        }

        // ---------- Validierung (IST-Verhalten dokumentiert) ----------

        [Fact]
        public void ObjectID_Null_Wirft_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                UavTalkBuilder.Build(UavTalkMessageType.ObjectData, null, null, new byte[0]));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        [InlineData(5)]
        public void ObjectID_Falsche_Laenge_Wirft_ArgumentException(int laenge)
        {
            Assert.Throws<ArgumentException>(() =>
                UavTalkBuilder.Build(UavTalkMessageType.ObjectData, new byte[laenge], null, new byte[0]));
        }

        [Fact]
        public void InstanceID_Falsche_Laenge_Wirft_ArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                UavTalkBuilder.Build(UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand,
                    new byte[] { 0x01 }, new byte[0]));
        }

        [Fact]
        public void Daten_Null_Wird_Als_Leer_Behandelt()
        {
            byte[] nachricht = UavTalkBuilder.Build(
                UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand, null, null);
            Assert.Equal(10, nachricht.Length); // nur Header
            Assert.Equal("0A 00", Bytes(new byte[] { nachricht[2], nachricht[3] }));
        }

        [Fact]
        public void Nachricht_Ueber_Int16_Limit_Wirft_ArgumentException()
        {
            // 10 + 32758 = 32768 > short.MaxValue -> Abweisung (dokumentiertes IST-Verhalten)
            Assert.Throws<ArgumentException>(() =>
                UavTalkBuilder.Build(UavTalkMessageType.ObjectData, ObjectIds.ManualControlCommand,
                    null, new byte[32758]));
        }
    }
}
