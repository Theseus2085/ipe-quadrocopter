using System;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T3 (TEST-AUFTRAG): ManualControlCommandData — Bytematrix des 46-Byte-Payloads.
    /// Feldreihenfolge und -bedeutung aus wissen §6 / E2 (fest):
    /// Throttle, Roll, Pitch, Yaw, Collective(=0), Thrust(=Throttle),
    /// Channel int16[10](=0), Connected, FlightModeSwitchPosition(=0).
    /// Erwartete Float-Bytes sind unabhaengige IEEE-754-Literale.
    /// </summary>
    public class T3ManualControlCommandMatrixTests
    {
        private static string Bytes(byte[] b) { return BitConverter.ToString(b).Replace("-", " "); }

        private static byte[] PayloadVon(ManualControlCommandData cmd) { return cmd.ToPayload(); }

        private static string Fenster(byte[] payload, int start, int laenge)
        {
            return BitConverter.ToString(payload, start, laenge).Replace("-", " ");
        }

        // ---------- Konstanten (Freeze) ----------

        [Fact]
        public void PayloadLaenge_Ist_46_Bytes()
        {
            // 6*4 (float32) + 10*2 (int16) + 1 + 1 = 46 (wissen §6)
            Assert.Equal(46, ManualControlCommandData.PayloadLength);
        }

        [Fact]
        public void ChannelAnzahl_Ist_10()
        {
            Assert.Equal(10, ManualControlCommandData.ChannelCount);
        }

        [Fact]
        public void ToPayload_Liefert_Immer_Genau_46_Bytes()
        {
            var neutral = ManualControlCommandData.Neutral();
            var arming = ManualControlCommandData.ForArming();
            var steuernd = new ManualControlCommandData { Throttle = 0.75f, Roll = -0.25f };
            Assert.Equal(46, PayloadVon(neutral).Length);
            Assert.Equal(46, PayloadVon(arming).Length);
            Assert.Equal(46, PayloadVon(steuernd).Length);
        }

        // ---------- Jede Byte-Position einzeln (volle Matrix) ----------

        [Fact]
        public void Alle_46_Positionen_Eines_Konkreten_Kommandos_Byte_exakt()
        {
            // Throttle=1.0, Roll=-1.0, Pitch=0.5, Yaw=0.0, Collective=0.25 (wird 0? NEIN:
            // Collective bleibt wie gesetzt — hier 0 setzen), Connected=1, FMSP=0.
            var cmd = new ManualControlCommandData
            {
                Throttle = 1.0f,   // 00 00 80 3F
                Roll = -1.0f,      // 00 00 80 BF
                Pitch = 0.5f,      // 00 00 00 3F
                Yaw = 0.0f,        // 00 00 00 00
                Collective = 0.0f, // 00 00 00 00
                Connected = 1,
                FlightModeSwitchPosition = 0
            };
            byte[] p = PayloadVon(cmd);
            Assert.Equal("00 00 80 3F 00 00 80 BF 00 00 00 3F 00 00 00 00 00 00 00 00 00 00 80 3F"
                + " 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00", Bytes(p));
        }

        [Fact]
        public void Feldreihenfolge_Positionen_0_bis_23_Mit_Verschiedenen_Werten()
        {
            // Sechs UNTERSCHIEDLICHE Werte: jede Position eindeutig zugeordbar.
            var cmd = new ManualControlCommandData
            {
                Throttle = 0.1f,   // CD CC CC 3D
                Roll = 0.2f,       // CD CC 4C 3E
                Pitch = 0.3f,      // 9A 99 99 3E
                Yaw = 0.4f,        // CD CC CC 3E
                Collective = 0.25f,// 00 00 80 3E
                Thrust = 0.75f     // wird von ToPayload mit Throttle ueberschrieben -> CD CC CC 3D
            };
            byte[] p = PayloadVon(cmd);
            Assert.Equal("CD CC CC 3D", Fenster(p, 0, 4));   // 1) Throttle
            Assert.Equal("CD CC 4C 3E", Fenster(p, 4, 4));   // 2) Roll
            Assert.Equal("9A 99 99 3E", Fenster(p, 8, 4));   // 3) Pitch
            Assert.Equal("CD CC CC 3E", Fenster(p, 12, 4));  // 4) Yaw
            Assert.Equal("00 00 80 3E", Fenster(p, 16, 4));  // 5) Collective
            Assert.Equal("CD CC CC 3D", Fenster(p, 20, 4));  // 6) Thrust = Throttle-Spiegel
        }

        [Fact]
        public void Thrust_Wird_In_ToPayload_Auf_Throttle_Gespiegelt()
        {
            // IST-Verhalten (wissen §6: "Thrust immer gleich Throttle-Wert"):
            // ToPayload() ueberschreibt ein abweichendes Thrust-Feld UND mutiert das Objekt.
            var cmd = new ManualControlCommandData { Throttle = 0.75f, Thrust = 1234.5f };
            byte[] p = PayloadVon(cmd);
            Assert.Equal(Fenster(p, 0, 4), Fenster(p, 20, 4)); // Bytefenster identisch
            Assert.Equal(0.75f, cmd.Thrust);                   // Feld nachgezogen
        }

        [Fact]
        public void Channel_Block_Positionen_24_bis_43_Ist_Absolut_Null()
        {
            var cmd = new ManualControlCommandData
            {
                Throttle = 1.0f, Roll = 1.0f, Pitch = 1.0f, Yaw = 1.0f, Connected = 1
            };
            byte[] p = PayloadVon(cmd);
            for (int i = 24; i <= 43; i++)
            {
                Assert.Equal(0x00, p[i]); // int16[10] = 20 Nullbytes, auch bei voll ausgelenkter Steuerung
            }
        }

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(5, "05")] // laut wissen §6 ist 00...05 moeglich, 00 bei Lehrstuhlkonfiguration
        public void Connected_Liegt_An_Position_44(byte wert, string erwartet)
        {
            var cmd = new ManualControlCommandData { Connected = wert };
            Assert.Equal(erwartet, Fenster(PayloadVon(cmd), 44, 1));
        }

        [Theory]
        [InlineData(0, "00")]
        [InlineData(1, "01")]
        [InlineData(5, "05")]
        public void FlightModeSwitchPosition_Liegt_An_Position_45(byte wert, string erwartet)
        {
            var cmd = new ManualControlCommandData { FlightModeSwitchPosition = wert };
            Assert.Equal(erwartet, Fenster(PayloadVon(cmd), 45, 1));
        }

        // ---------- Neutral() ----------

        [Fact]
        public void Neutral_Liefert_46_Nullbytes()
        {
            byte[] p = PayloadVon(ManualControlCommandData.Neutral());
            for (int i = 0; i < 46; i++)
            {
                Assert.Equal(0x00, p[i]); // PROG-011: Throttle=0, nichts klebt
            }
        }

        [Fact]
        public void Neutral_Setzt_Alle_Felder_Auf_Null()
        {
            var n = ManualControlCommandData.Neutral();
            Assert.Equal(0f, n.Throttle);
            Assert.Equal(0f, n.Roll);
            Assert.Equal(0f, n.Pitch);
            Assert.Equal(0f, n.Yaw);
            Assert.Equal(0f, n.Collective);
            Assert.Equal(0f, n.Thrust);
            Assert.Equal(0, n.Connected);
            Assert.Equal(0, n.FlightModeSwitchPosition);
        }

        // ---------- ForArming() (E5) ----------

        [Fact]
        public void ForArming_Throttle_Minus1_Thrust_Minus1_Rest_Neutral()
        {
            byte[] p = PayloadVon(ManualControlCommandData.ForArming());
            Assert.Equal("00 00 80 BF", Fenster(p, 0, 4));   // Throttle = -1
            Assert.Equal("00 00 80 BF", Fenster(p, 20, 4));  // Thrust = -1 (E5: BEIDE -1!)
            Assert.Equal("00 00 00 00", Fenster(p, 4, 4));   // Roll = 0
            Assert.Equal("00 00 00 00", Fenster(p, 8, 4));   // Pitch = 0
            Assert.Equal("00 00 00 00", Fenster(p, 12, 4));  // Yaw = 0
            Assert.Equal("00 00 00 00", Fenster(p, 16, 4));  // Collective = 0
            Assert.Equal("00", Fenster(p, 44, 1));           // Connected = 0
            Assert.Equal("00", Fenster(p, 45, 1));           // FlightModeSwitchPosition = 0
        }

        [Fact]
        public void ForArming_Felder_Auf_ObjektEbene_Throttle_Und_Thrust_Minus1()
        {
            var cmd = ManualControlCommandData.ForArming();
            Assert.Equal(-1f, cmd.Throttle);
            Assert.Equal(-1f, cmd.Thrust);
        }

        // ---------- Wertegrenzen -1 / 0 / +1 (wissen §6 Wertebereiche) ----------

        [Theory]
        [InlineData(-1f, "00 00 80 BF")]
        [InlineData(0f, "00 00 00 00")]
        [InlineData(1f, "00 00 80 3F")]
        public void Throttle_An_Wertegrenzen_Byte_exakt(float wert, string erwartet)
        {
            var cmd = new ManualControlCommandData { Throttle = wert };
            Assert.Equal(erwartet, Fenster(PayloadVon(cmd), 0, 4));
        }

        [Theory]
        [InlineData(-1f, "00 00 80 BF")]
        [InlineData(0f, "00 00 00 00")]
        [InlineData(1f, "00 00 80 3F")]
        public void Roll_An_Wertegrenzen_Byte_exakt(float wert, string erwartet)
        {
            var cmd = new ManualControlCommandData { Roll = wert };
            Assert.Equal(erwartet, Fenster(PayloadVon(cmd), 4, 4));
        }

        [Theory]
        [InlineData(-1f, "00 00 80 BF")]
        [InlineData(0f, "00 00 00 00")]
        [InlineData(1f, "00 00 80 3F")]
        public void Pitch_An_Wertegrenzen_Byte_exakt(float wert, string erwartet)
        {
            var cmd = new ManualControlCommandData { Pitch = wert };
            Assert.Equal(erwartet, Fenster(PayloadVon(cmd), 8, 4));
        }

        [Theory]
        [InlineData(-1f, "00 00 80 BF")]
        [InlineData(0f, "00 00 00 00")]
        [InlineData(1f, "00 00 80 3F")]
        public void Yaw_An_Wertegrenzen_Byte_exakt(float wert, string erwartet)
        {
            var cmd = new ManualControlCommandData { Yaw = wert };
            Assert.Equal(erwartet, Fenster(PayloadVon(cmd), 12, 4));
        }

        // ---------- Ausserhalb des Wertebereichs: dokumentiertes IST-Verhalten ----------

        [Fact]
        public void Werte_Ausserhalb_Des_Bereichs_Werden_Ungefiltert_Serialisiert()
        {
            // DOKUMENTIERTES IST-VERHALTEN: ToPayload() klemmt (clamped) NICHT und wirft
            // KEINE Exception — der Wert wird als IEEE-754-Bitmuster ausgegeben, wie er
            // gesetzt wurde. Die Bereichswahrung liegt beim Aufrufer (ControllerMapping
            // clamped). 2.0f = 0x40000000 -> "00 00 00 40", -2.0f -> "00 00 00 C0".
            var cmd = new ManualControlCommandData { Throttle = 2.0f, Roll = -2.0f };
            byte[] p = PayloadVon(cmd);
            Assert.Equal("00 00 00 40", Fenster(p, 0, 4));
            Assert.Equal("00 00 00 C0", Fenster(p, 4, 4));
        }
    }
}
