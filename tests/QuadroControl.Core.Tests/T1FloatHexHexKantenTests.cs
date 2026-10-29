using System;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T1 (TEST-AUFTRAG): Kanten der Float-/Hex-Umwandlung.
    /// Alle erwarteten Bitmuster sind UNABHAENGIG vom Code als IEEE-754-Muster
    /// berechnet (wissen-programmierung.md §5: IEEE 754, Little Endian).
    /// Ausnahme-Tests dokumentieren das IST-Verhalten (Exception-Typ).
    /// </summary>
    public class T1FloatHexHexKantenTests
    {
        // ---------- IEEE-754-Kanten: bekannte Bitmuster ----------

        [Theory]
        [InlineData(float.NaN, "00 00 C0 FF")]              // Single.NaN = 0xFFC00000 (signiert!)
        [InlineData(float.PositiveInfinity, "00 00 80 7F")] // 0x7F800000
        [InlineData(float.NegativeInfinity, "00 00 80 FF")] // 0xFF800000
        [InlineData(float.MaxValue, "FF FF 7F 7F")]         // 0x7F7FFFFF
        [InlineData(float.MinValue, "FF FF 7F FF")]         // 0xFF7FFFFF
        public void Ieee754_Kanten_Liefern_Unabhaengig_Berechnetes_Bitmuster(float wert, string erwartet)
        {
            Assert.Equal(erwartet, FloatHex.ToHexString(wert));
        }

        [Fact]
        public void Positives_QuietNaN_Bitmuster_Wird_Erhalten()
        {
            // 0x7FC00000 = NaN ohne Vorzeichenbit — auch dieses Muster muss
            // unveraendert durch die Umwandlung (keine NaN-Kanonisierung).
            float wert = BitConverter.Int32BitsToSingle(0x7FC00000);
            Assert.Equal("00 00 C0 7F", FloatHex.ToHexString(wert));
        }

        [Fact]
        public void PositivNull_Liefert_Vier_Nullbytes()
        {
            Assert.Equal("00 00 00 00", FloatHex.ToHexString(0.0f));
        }

        [Fact]
        public void NegativNull_Bleibt_Erhalten_Nicht_Kanonisiert()
        {
            // -0.0f = 0x80000000 -> Little Endian "00 00 00 80".
            // Protokolleigenschaft: das Vorzeichenbit darf nicht verloren gehen.
            Assert.Equal("00 00 00 80", FloatHex.ToHexString(-0.0f));
        }

        [Fact]
        public void Kleinste_Denormale_Epsilon_Liefert_01_00_00_00()
        {
            // float.Epsilon = 2^-45 = 0x00000001 (denormal)
            Assert.Equal("01 00 00 00", FloatHex.ToHexString(float.Epsilon));
        }

        [Fact]
        public void Groesste_Denormale_Liefert_FF_FF_7F_00()
        {
            // groesste denormale Zahl = 0x007FFFFF
            float wert = BitConverter.Int32BitsToSingle(0x007FFFFF);
            Assert.Equal("FF FF 7F 00", FloatHex.ToHexString(wert));
        }

        [Fact]
        public void Denormale_Mittendrin_Bleibt_Erhalten()
        {
            // 3 * Epsilon = 0x00000003 (denormal, kein Normalisieren erlaubt)
            float wert = BitConverter.Int32BitsToSingle(0x00000003);
            Assert.Equal("03 00 00 00", FloatHex.ToHexString(wert));
            Assert.Equal(float.Epsilon * 3, wert); // dort noch exakt darstellbar
        }

        // ---------- Roundtrip mit deterministischem Seed ----------

        [Fact]
        public void Roundtrip_2000_Zufallsfloats_Bitgleich()
        {
            // Deterministischer Seed (TEST-AUFTRAG: mind. 1000 Werte, Seed fest).
            var rng = new Random(20261029);
            for (int i = 0; i < 2000; i++)
            {
                int bits = rng.Next(int.MinValue, int.MaxValue); // voller 32-Bit-Raum inkl. NaN/Inf/Denormale
                float wert = BitConverter.Int32BitsToSingle(bits);

                byte[] littleEndian = FloatHex.ToBytes(wert);
                Assert.Equal(4, littleEndian.Length);

                float zurueck = FloatHex.FromBytes(littleEndian);
                Assert.Equal(bits, BitConverter.SingleToInt32Bits(zurueck)); // BIT-gleich, nicht nur ==
            }
        }

        [Fact]
        public void Roundtrip_Kantenwerte_Bitgleich()
        {
            // Kanten, bei denen ein einfacher ==-Vergleich nicht reicht (NaN!)
            int[] bitmuster =
            {
                0x7FC00000,               // NaN
                unchecked((int)0xFFC00000), // -NaN
                0x7F800000,               // +Inf
                unchecked((int)0xFF800000), // -Inf
                unchecked((int)0x80000000), // -0.0
                0x00000000,               // +0.0
                0x00000001,               // Epsilon
                0x007FFFFF,               // groesste denormale
                0x7F7FFFFF,               // MaxValue
                unchecked((int)0xFF7FFFFF) // MinValue
            };
            foreach (int bits in bitmuster)
            {
                float wert = BitConverter.Int32BitsToSingle(bits);
                float zurueck = FloatHex.FromBytes(FloatHex.ToBytes(wert));
                Assert.Equal(bits, BitConverter.SingleToInt32Bits(zurueck));
            }
        }

        [Fact]
        public void ToHexString_Ist_Ueber_Hex_Parse_Zurueckparsebar()
        {
            var rng = new Random(20261029);
            for (int i = 0; i < 1000; i++)
            {
                float wert = BitConverter.Int32BitsToSingle(rng.Next(int.MinValue, int.MaxValue));
                byte[] erwartet = FloatHex.ToBytes(wert);
                byte[] geparst = Hex.Parse(FloatHex.ToHexString(wert));
                Assert.Equal(erwartet, geparst);
            }
        }

        // ---------- Hex.ToHexString: Format ----------

        [Fact]
        public void ToHexString_Grossbuchstaben_Mit_Einem_Leerzeichen()
        {
            Assert.Equal("C4 10 74 80", Hex.ToHexString(new byte[] { 0xC4, 0x10, 0x74, 0x80 }));
        }

        [Fact]
        public void ToHexString_LeeresArray_Liefert_LeerenString()
        {
            Assert.Equal(string.Empty, Hex.ToHexString(new byte[0]));
        }

        [Fact]
        public void ToHexString_Null_Wirft_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Hex.ToHexString(null));
        }

        // ---------- Hex.Parse: Validierung (IST-Verhalten dokumentiert) ----------

        [Fact]
        public void Parse_Null_Wirft_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Hex.Parse(null));
        }

        [Theory]
        [InlineData("3c 20", 0x3C, 0x20)]   // Kleinschreibung wie in den VL5-Metadaten
        [InlineData("3C 20", 0x3C, 0x20)]   // Grossschreibung
        [InlineData("3c 2A", 0x3C, 0x2A)]   // gemischte Schreibweise
        [InlineData("3C 2a", 0x3C, 0x2A)]
        public void Parse_Ist_Gross_Klein_Unabhaengig(string hex, byte b0, byte b1)
        {
            Assert.Equal(new byte[] { b0, b1 }, Hex.Parse(hex));
        }

        [Fact]
        public void Parse_Toleriert_Mehrere_Leerzeichen_Tabs_Zeilenweise()
        {
            // Dokumentiertes IST-Verhalten: Split ueber ' ', '\t', '\r', '\n', ','
            byte[] ergebnis = Hex.Parse("  3C\t20\r\n30  40 ");
            Assert.Equal(new byte[] { 0x3C, 0x20, 0x30, 0x40 }, ergebnis);
        }

        [Fact]
        public void Parse_Toleriert_Komma_Als_Trennzeichen()
        {
            Assert.Equal(new byte[] { 0x3C, 0x20 }, Hex.Parse("3C,20"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Parse_Leerstring_Und_Nur_Whitespace_Liefern_LeeresArray(string hex)
        {
            Assert.Equal(new byte[0], Hex.Parse(hex));
        }

        [Fact]
        public void Parse_NichtHexZeichen_Wirft_FormatException()
        {
            // IST-Verhalten: Convert.ToByte("GG", 16) -> FormatException
            Assert.Throws<FormatException>(() => Hex.Parse("GG"));
        }

        [Fact]
        public void Parse_Token_Mit_Drei_Hexziffern_Ueber_255_Wirft_OverflowException()
        {
            // "ABC" = 0xABC = 2748 > 255 -> OverflowException (dokumentiertes IST-Verhalten)
            Assert.Throws<OverflowException>(() => Hex.Parse("ABC"));
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("A", 10)]
        [InlineData("F", 15)]
        [InlineData("ff", 255)]
        public void Parse_Einzelne_Hexziffer_Ist_Zulaessig(string token, byte erwartet)
        {
            // IST-Verhalten: einstellige Tokens werden akzeptiert (keine ungrade-
            // Ziffern-Paadung erzwungen)
            Assert.Equal(new byte[] { erwartet }, Hex.Parse(token));
        }

        // ---------- Hex.SameBytes ----------

        [Fact]
        public void SameBytes_Ignoriert_Grossschreibung_Und_Whitespace()
        {
            Assert.True(Hex.SameBytes("3c 20 12 00", "3C  20\t12\n00"));
        }

        [Fact]
        public void SameBytes_Unterschiedliche_Bytes_Liefert_False()
        {
            Assert.False(Hex.SameBytes("3C 20", "3C 21"));
        }

        [Fact]
        public void SameBytes_Unterschiedliche_Laenge_Liefert_False()
        {
            Assert.False(Hex.SameBytes("3C 20", "3C"));
        }

        // ---------- Int16-Kanten (Channel-Felder, wissen §6 Feld 7) ----------

        [Fact]
        public void Int16_MinValue_Liefert_00_80()
        {
            // -32768 = 0x8000 -> Little Endian "00 80"
            Assert.Equal("00 80", FloatHex.Int16ToHexString(short.MinValue));
        }

        [Fact]
        public void Int16_MaxValue_Liefert_FF_7F()
        {
            Assert.Equal("FF 7F", FloatHex.Int16ToHexString(short.MaxValue));
        }

        [Fact]
        public void Int16_Zero_Liefert_Nullbytes()
        {
            Assert.Equal(new byte[] { 0x00, 0x00 }, FloatHex.Int16ToBytes(0));
            Assert.Equal("00 00", FloatHex.Int16ToHexString(0));
        }

        // ---------- FloatHex.FromBytes: Validierung ----------

        [Fact]
        public void FromBytes_Null_Wirft_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => FloatHex.FromBytes(null));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(5)]
        public void FromBytes_Falsche_Laenge_Wirft_ArgumentException(int laenge)
        {
            Assert.Throws<ArgumentException>(() => FloatHex.FromBytes(new byte[laenge]));
        }

        [Fact]
        public void FromBytes_Bekannter_Vektor_1_0()
        {
            Assert.Equal(1.0f, FloatHex.FromBytes(new byte[] { 0x00, 0x00, 0x80, 0x3F }));
        }

        [Fact]
        public void FromBytes_Kleinste_Denormale_Liefert_Epsilon()
        {
            Assert.Equal(float.Epsilon, FloatHex.FromBytes(new byte[] { 0x01, 0x00, 0x00, 0x00 }));
        }
    }
}
