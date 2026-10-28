using System;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Known-Vektor-Tests für die Float-Umwandlung (IEEE 754, Little Endian).
    /// Vektor 1.0f → "00 00 80 3F" ist im BUILD-AUFTRAG eingefroren;
    /// 100.0f entspricht dem VL5-Beispiel (42 C8 00 00 → LE 00 00 C8 42).
    /// </summary>
    public class FloatHexTests
    {
        [Fact]
        public void Float_1_0_Liefert_Vektor_00_00_80_3F()
        {
            Assert.Equal("00 00 80 3F", FloatHex.ToHexString(1.0f));
        }

        [Fact]
        public void Float_Minus1_0_Liefert_00_00_80_BF()
        {
            Assert.Equal("00 00 80 BF", FloatHex.ToHexString(-1.0f));
        }

        [Fact]
        public void Float_0_0_Liefert_Nullbytes()
        {
            Assert.Equal("00 00 00 00", FloatHex.ToHexString(0.0f));
        }

        [Fact]
        public void Float_0_5_Liefert_00_00_00_3F()
        {
            Assert.Equal("00 00 00 3F", FloatHex.ToHexString(0.5f));
        }

        [Fact]
        public void Float_100_0_Entspricht_VL5_Beispiel_42_C8_00_00_LittleEndian()
        {
            // wissen §5: 100 (float) = 42 C8 00 00 → Little Endian: 00 00 C8 42
            Assert.Equal("00 00 C8 42", FloatHex.ToHexString(100.0f));
        }

        [Fact]
        public void FromBytes_Invertiert_ToBytes()
        {
            float[] werte = { 1.0f, -1.0f, 0.25f, 0.5f, 123.456f, -0.7071f };
            foreach (float wert in werte)
            {
                Assert.Equal(wert, FloatHex.FromBytes(FloatHex.ToBytes(wert)));
            }
        }

        [Fact]
        public void Int16_1000_Liefert_E8_03_LittleEndian()
        {
            Assert.Equal("E8 03", FloatHex.Int16ToHexString(1000));
        }

        [Fact]
        public void Int16_Minus1_Liefert_FF_FF()
        {
            Assert.Equal("FF FF", FloatHex.Int16ToHexString(-1));
        }

        [Fact]
        public void FromBytes_Lehnt_Falsche_Laenge_Ab()
        {
            Assert.Throws<ArgumentException>(() => FloatHex.FromBytes(new byte[] { 1, 2, 3 }));
        }
    }
}
