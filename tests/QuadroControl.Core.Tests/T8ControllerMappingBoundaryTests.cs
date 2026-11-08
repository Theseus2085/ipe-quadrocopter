using System;
using QuadroControl.Core.Controller;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T8 (TEST-AUFTRAG): ControllerMapping — exakte Grenzwerte der XInput-Achsen
    /// (-32768, 0, +32767) und des rechten Triggers (0, Totzone, 255),
    /// D3v2-Zuordnung konsistent mit docs/controllerbelegung.md
    /// (R2=GAS, linksX=Gieren, rechtsX=Rollen, rechtsY=Nicken),
    /// Neutralstellung, Monotonie/keine Spruenge.
    /// </summary>
    public class T8ControllerMappingBoundaryTests
    {
        // ---------- Konstanten (XInput-Grenzen) ----------

        [Fact]
        public void AxisMax_Ist_32767()
        {
            Assert.Equal(32767, ControllerMapping.AxisMax);
        }

        [Fact]
        public void TriggerMax_Ist_255()
        {
            Assert.Equal(255, ControllerMapping.TriggerMax);
        }

        [Fact]
        public void TriggerTotzone_Ist_10()
        {
            Assert.Equal(10, ControllerMapping.TriggerTotzone);
        }

        // ---------- Throttle (R2, rechter Trigger): 0..1 ----------

        [Fact]
        public void Throttle_Ruhe_0_Liefert_Exakt_0()
        {
            Assert.Equal(0f, ControllerMapping.ThrottleFromTrigger(0));
        }

        [Fact]
        public void Throttle_Totzonenrand_10_Liefert_Exakt_0()
        {
            Assert.Equal(0f, ControllerMapping.ThrottleFromTrigger(10));
        }

        [Fact]
        public void Throttle_Erster_Wirksamer_Wert_11_Liefert_Exakt_Dokumentformel()
        {
            // controllerbelegung.md (bindende Formel): (t-10)/(255-10) fuer t > 10.
            // t=11 -> 1/245 = 0.004081...
            float wert = ControllerMapping.ThrottleFromTrigger(11);
            Assert.Equal(1f / 245f, wert); // Dokumentformel
            Assert.InRange(wert, 0.004f, 0.005f); // unabhaengige Abschaetzung
        }

        [Fact]
        public void Throttle_Oberer_Anschlag_255_Liefert_Exakt_1()
        {
            // (255-10)/245 = 245/245 = exakt 1.0
            Assert.Equal(1f, ControllerMapping.ThrottleFromTrigger(255));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(255)]
        public void Throttle_Bleibt_Im_Bereich_0_bis_1(byte roh)
        {
            float wert = ControllerMapping.ThrottleFromTrigger(roh);
            Assert.True(wert >= 0f && wert <= 1f, "Throttle ausserhalb [0;1]: " + wert);
        }

        // ---------- Roll/Pitch/Yaw: -1..1 ----------

        [Fact]
        public void Achse_Oberer_Anschlag_32767_Liefert_Exakt_1()
        {
            Assert.Equal(1f, ControllerMapping.AxisToNormalized(32767));
        }

        [Fact]
        public void Achse_Unterer_Wert_Minus32767_Liefert_Exakt_Minus1()
        {
            Assert.Equal(-1f, ControllerMapping.AxisToNormalized(-32767));
        }

        [Fact]
        public void Achse_Minus32768_Wird_Auf_Minus1_Geklemmt()
        {
            // -32768/32767 = -1.0000305 -> Clamp auf -1 (dokumentiertes Verhalten)
            Assert.Equal(-1f, ControllerMapping.AxisToNormalized(-32768));
        }

        [Fact]
        public void Achse_Neutralstellung_0_Liefert_Exakt_0()
        {
            Assert.Equal(0f, ControllerMapping.RollFromAxis(0));
            Assert.Equal(0f, ControllerMapping.PitchFromAxis(0));
            Assert.Equal(0f, ControllerMapping.YawFromAxis(0));
        }

        [Theory]
        [InlineData(-32768)]
        [InlineData(-12345)]
        [InlineData(0)]
        [InlineData(12345)]
        [InlineData(32767)]
        public void Achse_Bleibt_Im_Bereich_Minus1_bis_1(short roh)
        {
            Assert.True(Math.Abs(ControllerMapping.AxisToNormalized(roh)) <= 1f);
        }

        // ---------- Monotonie ueber Stuetzstellen (keine Spruenge) ----------

        [Fact]
        public void Throttle_Ist_Monoton_Ueber_Den_Gesamten_Triggerbereich()
        {
            float vorher = ControllerMapping.ThrottleFromTrigger(0);
            for (int roh = 1; roh <= 255; roh++)
            {
                float wert = ControllerMapping.ThrottleFromTrigger((byte)roh);
                Assert.True(wert >= vorher, "Nicht monoton bei Triggerwert " + roh);
                Assert.True(wert - vorher <= 1.1f / 245f, "Sprung bei Triggerwert " + roh); // 1/245 ~ 4.08e-3
                vorher = wert;
            }
        }

        [Fact]
        public void Achse_Ist_Monoton_Ueber_Den_Gesamten_Achsbereich()
        {
            float vorher = ControllerMapping.AxisToNormalized(short.MinValue); // = -1 (geklemmt)
            for (int roh = short.MinValue + 1; roh <= short.MaxValue; roh++)
            {
                float wert = ControllerMapping.AxisToNormalized((short)roh);
                Assert.True(wert >= vorher, "Nicht monoton bei Rohwert " + roh);
                Assert.True(wert - vorher <= 4e-5f, "Sprung bei Rohwert " + roh); // 1/32767 ~ 3.05e-5
                vorher = wert;
            }
        }

        // ---------- D3v2-Zuordnung (docs/controllerbelegung.md) ----------

        [Fact]
        public void D3v2_R2_Ist_Throttle_Und_Thrust()
        {
            // Trigger voll gezogen -> Throttle/Thrust = 1, Sticks neutral
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, 255, true);
            Assert.Equal(1f, cmd.Throttle); // R2 voll = voll Gas
            Assert.Equal(cmd.Throttle, cmd.Thrust); // Spiegel-Regel (wissen §6)
        }

        [Fact]
        public void D3v2_R2_Losgelassen_Ist_0_Gas()
        {
            // Sicherheit: Trigger loslassen = 0 Gas (kein klebender Wert)
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, 0, true);
            Assert.Equal(0f, cmd.Throttle);
            Assert.Equal(0f, cmd.Thrust);
        }

        [Fact]
        public void D3v2_Linker_Stick_X_Ist_Yaw()
        {
            var cmd = ControllerMapping.ToManualControlCommand(-32767, 0, 0, 0, true);
            Assert.Equal(-1f, cmd.Yaw);
            Assert.Equal(0f, cmd.Roll);  // andere Felder unberuehrt
            Assert.Equal(0f, cmd.Pitch);
            Assert.Equal(0f, cmd.Throttle);
        }

        [Fact]
        public void D3v2_Rechter_Stick_X_Ist_Roll()
        {
            var cmd = ControllerMapping.ToManualControlCommand(0, 32767, 0, 0, true);
            Assert.Equal(1f, cmd.Roll); // rechtsX -> Rollen
            Assert.Equal(0f, cmd.Yaw);
            Assert.Equal(0f, cmd.Pitch);
        }

        [Fact]
        public void D3v2_Rechter_Stick_Y_Ist_Pitch()
        {
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, -32767, 0, true);
            Assert.Equal(-1f, cmd.Pitch); // rechtsY -> Nicken
            Assert.Equal(0f, cmd.Roll);
            Assert.Equal(0f, cmd.Yaw);
        }

        [Fact]
        public void Throttle_Mittelstellung_128_Ist_Ca_Halbes_Gas()
        {
            // Der linke Stick Y existiert im Kommando-Aufruf nicht mehr (D3v2):
            // Signatur ist (leftX, rightX, rightY, trigger); Throttle kommt nur aus R2.
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, 128, true);
            Assert.InRange(cmd.Throttle, 0.47f, 0.49f); // (128-10)/245 ≈ 0.482
        }

        [Fact]
        public void D3v2_Volle_Kombination_Jede_Eingabe_Ihr_Eigenes_Feld()
        {
            // Alle Eingaben gleichzeitig, alle verschieden:
            // linksX=-32767 (Yaw=-1), rechtsX=32767 (Roll=1),
            // rechtsY=-32767 (Pitch=-1), R2=255 (Throttle=1)
            var cmd = ControllerMapping.ToManualControlCommand(-32767, 32767, -32767, 255, true);
            Assert.Equal(1f, cmd.Throttle);
            Assert.Equal(1f, cmd.Roll);
            Assert.Equal(-1f, cmd.Pitch);
            Assert.Equal(-1f, cmd.Yaw);
        }

        [Fact]
        public void Trigger_Veraendert_Nur_Throttle_Thrust_Nicht_Die_Achsen()
        {
            var wenig = ControllerMapping.ToManualControlCommand(12345, -5432, 6789, 20, true);
            var viel = ControllerMapping.ToManualControlCommand(12345, -5432, 6789, 250, true);
            Assert.Equal(wenig.Roll, viel.Roll);
            Assert.Equal(wenig.Pitch, viel.Pitch);
            Assert.Equal(wenig.Yaw, viel.Yaw);
            Assert.True(viel.Throttle > wenig.Throttle);
        }

        // ---------- Command-Erzeugung: Fixfelder ----------

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public void Connected_Flag_Landet_Als_1_oder_0_Im_Kommando(bool verbunden, byte erwartet)
        {
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, 0, verbunden);
            Assert.Equal(erwartet, cmd.Connected);
        }

        [Fact]
        public void Erzeugtes_Kommando_Collective_0_und_FlightModeSwitchPosition_0()
        {
            var cmd = ControllerMapping.ToManualControlCommand(1000, 2000, 3000, 40, true);
            Assert.Equal(0f, cmd.Collective);
            Assert.Equal(0, cmd.FlightModeSwitchPosition);
        }

        [Fact]
        public void Erzeugtes_Kommando_Serialisiert_Mit_Thrust_Throttle_Spiegel()
        {
            // Integration: Mapping -> Payload; Thrust-Fenster == Throttle-Fenster
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, 191, true);
            byte[] payload = cmd.ToPayload();
            string throttle = BitConverter.ToString(payload, 0, 4);
            string thrust = BitConverter.ToString(payload, 20, 4);
            Assert.Equal(thrust, throttle);
            // Throttle(191) = (191-10)/245 = 181/245 ~ 0.7388 — im Rahmen 0.73..0.76
            Assert.InRange(cmd.Throttle, 0.73f, 0.76f);
        }
    }
}
