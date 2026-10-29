using System;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T5 (TEST-AUFTRAG): HandshakeSequenceProvider — exakt 5 Nachrichten in
    /// fester Reihenfolge (E3); die 2 Metadaten-Nachrichten byte-identisch mit
    /// wissen-programmierung.md §7 (wortwurtzlich aus VL5 Folie 39); keine
    /// CRC-Anhaengung. Die wissen-Strings sind hier NOCH EINMAL unabhaengig als
    /// Literale abgelegt — der Test vergleicht also nicht einen Konstanten-
    /// Verweis mit sich selbst.
    /// </summary>
    public class T5HandshakeMetadataTests
    {
        // wortwurtlich aus wissen-programmierung.md §7 (dort Kleinschreibung):
        private const string MccMetaWissen = "3c 20 12 00 81 74 10 c4 00 00 41 00 c0 07 64 00 00 00";
        private const string AccMetaWissen = "3c 20 12 00 5c 98 09 c4 00 00 41 00 d0 07 64 00 00 00";

        private static string Bytes(byte[] b) { return BitConverter.ToString(b).Replace("-", " "); }

        private static byte[] Feld(byte[] nachricht, int start, int laenge)
        {
            var feld = new byte[laenge];
            Array.Copy(nachricht, start, feld, 0, laenge);
            return feld;
        }

        // ---------- Anzahl und Reihenfolge ----------

        [Fact]
        public void MessageCount_Ist_5()
        {
            Assert.Equal(5, HandshakeSequenceProvider.MessageCount);
        }

        [Fact]
        public void GetMessages_Liefert_Exakt_5_Nachrichten()
        {
            Assert.Equal(5, HandshakeSequenceProvider.GetMessages().Length);
        }

        [Theory]
        [InlineData(0, 0x00)] // 1. GCSTelemetryStats Status 00 (Disconnected)
        [InlineData(1, 0x01)] // 2. GCSTelemetryStats Status 01 (HandshakeReq)
        [InlineData(2, 0x03)] // 3. GCSTelemetryStats Status 03 (Connected)
        public void Erste_Drei_Nachrichten_Sind_Gcs_Mit_Status_00_01_03(int index, byte status)
        {
            string hex = HandshakeSequenceProvider.GetMessages()[index];
            byte[] nachricht = Hex.Parse(hex);
            Assert.Equal(0x3C, nachricht[0]);                        // Sync
            Assert.Equal(0x20, nachricht[1]);                        // ObjectData
            Assert.Equal("0A DC D1 CA", Bytes(Feld(nachricht, 4, 4))); // GCSTelemetryStats LE
            Assert.Equal(47, nachricht.Length);                      // 10 + 37
            Assert.Equal(status, nachricht[46]);                     // letztes Payload-Byte = Status (10+36)
        }

        // ---------- Metadaten byte-identisch mit wissen §7 ----------

        [Fact]
        public void Nachricht_4_Ist_Die_Mcc_Metadaten_Wortwurtlich_Aus_wissen_Paragraph_7()
        {
            Assert.Equal(MccMetaWissen, HandshakeMetadata.ManualControlCommandMetadata);
        }

        [Fact]
        public void Nachricht_5_Ist_Die_AccessoryDesired_Metadaten_Wortwurtlich_Aus_wissen_Paragraph_7()
        {
            Assert.Equal(AccMetaWissen, HandshakeMetadata.AccessoryDesiredMetadata);
        }

        [Fact]
        public void GetMessages_Index_3_und_4_Sind_Die_Beiden_Metadaten()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            Assert.Equal(HandshakeMetadata.ManualControlCommandMetadata, messages[3]);
            Assert.Equal(HandshakeMetadata.AccessoryDesiredMetadata, messages[4]);
        }

        [Fact]
        public void Metadaten_Sind_Byte_Identisch_Zu_wissen_Paragraph_7_Unabhaengig_Von_Schreibweise()
        {
            string[] messages = HandshakeSequenceProvider.GetMessages();
            Assert.True(Hex.SameBytes(MccMetaWissen, messages[3]));
            Assert.True(Hex.SameBytes(AccMetaWissen, messages[4]));
        }

        // ---------- Struktur der Metadaten-Nachrichten ----------

        [Theory]
        [InlineData(3, "81 74 10 C4", "C0")] // MCC-Metadaten: ObjectID + Byte 12
        [InlineData(4, "5C 98 09 C4", "D0")] // AccessoryDesired-Metadaten: ObjectID + Byte 12
        public void Metadaten_Nachricht_Struktur_Sync_Type_Objekt_Instance(int index, string objektIdHex, string metaByteHex)
        {
            byte[] nachricht = Hex.Parse(HandshakeSequenceProvider.GetMessages()[index]);
            Assert.Equal(0x3C, nachricht[0]);                 // Sync
            Assert.Equal(0x20, nachricht[1]);                 // ObjectData
            Assert.Equal(objektIdHex, Bytes(Feld(nachricht, 4, 4)));
            Assert.Equal("00 00", Bytes(Feld(nachricht, 8, 2))); // InstanceID
            // Datenabschnitt 8 Bytes (10..17): beginnt in BEIDEN Metadaten mit 41 00,
            // unterscheidet sich erst in Byte 12 (C0 vs. D0) — wortwurtlich so in wissen §7.
            Assert.Equal(0x41, nachricht[10]);
            Assert.Equal(0x00, nachricht[11]);
            Assert.Equal(metaByteHex, Bytes(new byte[] { nachricht[12] }));
        }

        [Fact]
        public void Metadaten_ObjectIDs_Sind_Die_Meta_Objekte_Nicht_Die_Basis_Objekte()
        {
            // Die Metadaten zeigen auf die Meta-Objekte (wissen §6: ObjectIDMeta),
            // NICHT auf ManualControlCommand/AccessoryDesired selbst:
            // MCC       C4 10 74 80 -> Nachricht LE "80 74 10 C4"
            // Meta MCC  C4 10 74 81 -> Nachricht LE "81 74 10 c4"
            // ACC       C4 09 98 5A -> Nachricht LE "5A 98 09 C4"
            // Meta ACC  C4 09 98 5C -> Nachricht LE "5c 98 09 c4"
            string[] messages = HandshakeSequenceProvider.GetMessages();
            Assert.NotEqual("80 74 10 C4", Bytes(Feld(Hex.Parse(messages[3]), 4, 4)));
            Assert.NotEqual("5A 98 09 C4", Bytes(Feld(Hex.Parse(messages[4]), 4, 4)));
        }

        // ---------- Keine CRC-Anhaengung ----------

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void Keine_Einzige_Handshake_Nachricht_Traegt_Eine_Checksumme(int index)
        {
            byte[] nachricht = Hex.Parse(HandshakeSequenceProvider.GetMessages()[index]);
            int lengthFeld = nachricht[2] | (nachricht[3] << 8);
            // Length-Feld = Header+Daten OHNE Checksumme. Wenn die Nachricht
            // genauso viele Bytes hat wie das Feld angibt, fehlt kein/kein
            // zusaetzliches Byte -> E1: CRC haengt ausschliesslich die DLL an.
            Assert.Equal(lengthFeld, nachricht.Length);
        }

        [Fact]
        public void Metadaten_Length_Feld_Ist_12_00_Passend_Zu_18_Bytes()
        {
            // 0x0012 = 18 = 10 Header + 8 Datenbytes
            byte[] mcc = Hex.Parse(HandshakeMetadata.ManualControlCommandMetadata);
            byte[] acc = Hex.Parse(HandshakeMetadata.AccessoryDesiredMetadata);
            Assert.Equal("12 00", Bytes(Feld(mcc, 2, 2)));
            Assert.Equal("12 00", Bytes(Feld(acc, 2, 2)));
            Assert.Equal(18, mcc.Length);
            Assert.Equal(18, acc.Length);
        }

        // ---------- Determinismus ----------

        [Fact]
        public void GetMessages_Liefert_Bei_Jedem_Aufruf_Dieselben_Bytes()
        {
            string[] erste = HandshakeSequenceProvider.GetMessages();
            string[] zweite = HandshakeSequenceProvider.GetMessages();
            Assert.Equal(erste, zweite);
        }
    }
}
