using System.Linq;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests für den ManualControlCommand-Datenabschnitt:
    /// 46 Bytes (4+4+4+4+4+4 float32 + 20 int16[10] + 1 + 1), feste
    /// Feldreihenfolge, Length-Feld 0x0038 → Little Endian "38 00".
    /// </summary>
    public class ManualControlCommandTests
    {
        private ManualControlCommandData Beispieldaten()
        {
            return new ManualControlCommandData
            {
                Throttle = -1f,
                Roll = 1f,
                Pitch = 0.5f,
                Yaw = 0.25f,
                Collective = 0f,
                Thrust = -1f,
                Connected = 1,
                FlightModeSwitchPosition = 0
            };
        }

        [Fact]
        public void Payload_Hat_Genau_46_Bytes()
        {
            Assert.Equal(46, Beispieldaten().ToPayload().Length);
        }

        [Fact]
        public void Komplette_Nachricht_Length_Feld_LittleEndian_38_00()
        {
            string hex = MessageFactory.BuildManualControlCommand(Beispieldaten());
            byte[] bytes = Hex.Parse(hex);

            // 10 Bytes Header + 46 Bytes Daten = 56 Bytes, Length = 0x0038
            Assert.Equal(56, bytes.Length);
            Assert.Equal(0x38, bytes[2]);
            Assert.Equal(0x00, bytes[3]);
        }

        [Fact]
        public void Feldreihenfolge_1_bis_6_Je_Float32_LittleEndian()
        {
            byte[] p = Beispieldaten().ToPayload();

            Assert.Equal("00 00 80 BF", Hex.ToHexString(p.Skip(0).Take(4).ToArray()));  // 1 Throttle = -1
            Assert.Equal("00 00 80 3F", Hex.ToHexString(p.Skip(4).Take(4).ToArray()));  // 2 Roll = 1
            Assert.Equal("00 00 00 3F", Hex.ToHexString(p.Skip(8).Take(4).ToArray()));  // 3 Pitch = 0.5
            Assert.Equal("00 00 80 3E", Hex.ToHexString(p.Skip(12).Take(4).ToArray())); // 4 Yaw = 0.25
            Assert.Equal("00 00 00 00", Hex.ToHexString(p.Skip(16).Take(4).ToArray())); // 5 Collective = 0
            Assert.Equal("00 00 80 BF", Hex.ToHexString(p.Skip(20).Take(4).ToArray())); // 6 Thrust = -1
        }

        [Fact]
        public void Feld_7_Channel_Int16_10_Ist_Immer_Null()
        {
            byte[] p = Beispieldaten().ToPayload();
            byte[] channel = p.Skip(24).Take(20).ToArray();
            Assert.All(channel, b => Assert.Equal(0, b));
        }

        [Fact]
        public void Feld_8_9_Connected_Und_FlightModeSwitchPosition()
        {
            byte[] p = Beispieldaten().ToPayload();
            Assert.Equal(1, p[44]); // Connected = 01 (für Flug benötigt)
            Assert.Equal(0, p[45]); // FlightModeSwitchPosition = 00 (Lehrstuhlkonfiguration)
        }

        [Fact]
        public void Thrust_Wird_Beim_Serialisieren_An_Throttle_Gekoppelt()
        {
            var cmd = new ManualControlCommandData { Throttle = 0.75f };
            byte[] p = cmd.ToPayload();

            Assert.Equal(0.75f, FloatHex.FromBytes(p.Skip(0).Take(4).ToArray()));
            Assert.Equal(0.75f, FloatHex.FromBytes(p.Skip(20).Take(4).ToArray())); // Thrust == Throttle
        }

        [Fact]
        public void Neutral_Alle_Werte_Null_Stillstand_Der_Rotoren()
        {
            byte[] p = ManualControlCommandData.Neutral().ToPayload();
            Assert.All(p, b => Assert.Equal(0, b));
        }

        [Fact]
        public void ForArming_Throttle_Und_Thrust_Minus1()
        {
            ManualControlCommandData cmd = ManualControlCommandData.ForArming();
            Assert.Equal(-1f, cmd.Throttle);
            Assert.Equal(-1f, cmd.Thrust);

            byte[] p = cmd.ToPayload();
            Assert.Equal("00 00 80 BF", Hex.ToHexString(p.Skip(0).Take(4).ToArray()));
            Assert.Equal("00 00 80 BF", Hex.ToHexString(p.Skip(20).Take(4).ToArray()));
        }

        [Fact]
        public void Nachricht_Beginnt_Mit_Header_ObjectID_80_74_10_C4()
        {
            string hex = MessageFactory.BuildManualControlCommand(ManualControlCommandData.Neutral());
            byte[] bytes = Hex.Parse(hex);
            Assert.Equal("3C 20", Hex.ToHexString(bytes.Take(2).ToArray()));
            Assert.Equal("80 74 10 C4 00 00", Hex.ToHexString(bytes.Skip(4).Take(6).ToArray()));
        }
    }
}
