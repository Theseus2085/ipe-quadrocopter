using System;
using QuadroControl.Core.Controller;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T8 (TEST-AUFTRAG): ControllerMapping — exakte Grenzwerte der XInput-Achsen
    /// (-32768, 0, +32767), Mode-2-Zuordnung konsistent mit
    /// docs/controllerbelegung.md (linksY=GAS, linksX=Gieren, rechtsX=Rollen,
    /// rechtsY=Nicken), Neutralstellung, Monotonie/keine Spruenge.
    /// HINWEIS: Trigger-Grenzwerte (0/128/255) sind in Core NICHT abgebildet —
    /// Trigger sind laut controllerbelegung.md nicht belegt (siehe ABWEICHUNGEN.md).
    /// </summary>
    public class T8ControllerMappingBoundaryTests
    {
        // ---------- Konstanten (XInput-Achsengrenzen) ----------

        [Fact]
        public void AxisMax_Ist_32767()
        {
            Assert.Equal(32767, ControllerMapping.AxisMax);
        }

        [Fact]
        public void AxisMin_Ist_Minus32768()
        {
            Assert.Equal(-32768, ControllerMapping.AxisMin);
        }

        // ---------- Throttle (linker Stick Y): 0..1 ----------

        [Fact]
        public void Throttle_Unterer_Anschlag_Minus32768_Liefert_Exakt_0()
        {
            Assert.Equal(0f, ControllerMapping.ThrottleFromAxis(-32768));
        }

        [Fact]
        public void Throttle_Oberer_Anschlag_32767_Liefert_Exakt_1()
        {
            // (32767+32768)/65535 = 65535/65535 = exakt 1.0 (float-exakt, da 65535=2^16-1)
            Assert.Equal(1f, ControllerMapping.ThrottleFromAxis(32767));
        }

        [Fact]
        public void Throttle_Neutralstellung_Roh_0_Liegt_Bei_Halbem_Gas_Nach_Dokumentformel()
        {
            // controllerbelegung.md (bindende Formel): (raw+32768)/65535.
            // Rohwert 0 -> 32768/65535 = 0.5000076... (float-arithmetisch exakt dieser Wert).
            float wert = ControllerMapping.ThrottleFromAxis(0);
            Assert.Equal(32768f / 65535f, wert); // Dokumentformel
            Assert.InRange(wert, 0.4999f, 0.5001f); // unabhaengige Abschaetzung: ~halbes Gas
        }

        [Theory]
        [InlineData(-32768)]
        [InlineData(-16384)]
        [InlineData(0)]
        [InlineData(16384)]
        [InlineData(32767)]
        public void Throttle_Bleibt_Im_Bereich_0_bis_1(short roh)
        {
            float wert = ControllerMapping.ThrottleFromAxis(roh);
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
        public void Throttle_Ist_Monoton_Ueber_Den_Gesamten_Achsbereich()
        {
            float vorher = ControllerMapping.ThrottleFromAxis(short.MinValue);
            for (int roh = short.MinValue + 1; roh <= short.MaxValue; roh++)
            {
                float wert = ControllerMapping.ThrottleFromAxis((short)roh);
                Assert.True(wert >= vorher, "Nicht monoton bei Rohwert " + roh);
                Assert.True(wert - vorher <= 1.6e-5f, "Sprung bei Rohwert " + roh); // 1/65535 ~ 1.53e-5
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

        // ---------- Mode-2-Zuordnung (docs/controllerbelegung.md) ----------

        [Fact]
        public void Mode2_Linker_Stick_Y_Ist_Throttle_Und_Thrust()
        {
            // Beide Staebewerte gleich -> nur linker Stick Y treibt Throttle/Thrust
            var cmd = ControllerMapping.ToManualControlCommand(0, 32767, 0, 0, true);
            Assert.Equal(1f, cmd.Throttle); // Stick voll oben = voll Gas
            Assert.Equal(cmd.Throttle, cmd.Thrust); // Spiegel-Regel (wissen §6)
        }

        [Fact]
        public void Mode2_Linker_Stick_X_Ist_Yaw()
        {
            var cmd = ControllerMapping.ToManualControlCommand(-32767, 0, 0, 0, true);
            Assert.Equal(-1f, cmd.Yaw);
            Assert.Equal(0f, cmd.Roll);  // andere Achsen unberuehrt
            Assert.Equal(0f, cmd.Pitch);
        }

        [Fact]
        public void Mode2_Rechter_Stick_X_Ist_Roll()
        {
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 32767, 0, true);
            Assert.Equal(1f, cmd.Roll); // rechtsX -> Rollen
            Assert.Equal(0f, cmd.Yaw);
            Assert.Equal(0f, cmd.Pitch);
        }

        [Fact]
        public void Mode2_Rechter_Stick_Y_Ist_Pitch()
        {
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, -32767, true);
            Assert.Equal(-1f, cmd.Pitch); // rechtsY -> Nicken
            Assert.Equal(0f, cmd.Roll);
            Assert.Equal(0f, cmd.Yaw);
        }

        [Fact]
        public void Mode2_Volle_Achskombination_Jede_Achse_Ihr_Eigenes_Feld()
        {
            // Alle vier Staebewerte gleichzeitig, alle verschieden:
            // linksX=-32767 (Yaw=-1), linksY=-32768 (Throttle=0),
            // rechtsX=32767 (Roll=1), rechtsY=-32767 (Pitch=-1)
            var cmd = ControllerMapping.ToManualControlCommand(-32767, -32768, 32767, -32767, true);
            Assert.Equal(0f, cmd.Throttle);
            Assert.Equal(1f, cmd.Roll);
            Assert.Equal(-1f, cmd.Pitch);
            Assert.Equal(-1f, cmd.Yaw);
        }

        [Fact]
        public void Achsen_Sind_Unabhaengig_Linker_Stick_Veraendert_RollPitch_Nicht()
        {
            var ohneAusschlag = ControllerMapping.ToManualControlCommand(0, 0, 12345, -5432, true);
            var mitAusschlag = ControllerMapping.ToManualControlCommand(-32767, 32767, 12345, -5432, true);
            Assert.Equal(ohneAusschlag.Roll, mitAusschlag.Roll);
            Assert.Equal(ohneAusschlag.Pitch, mitAusschlag.Pitch);
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
            var cmd = ControllerMapping.ToManualControlCommand(1000, 2000, 3000, 4000, true);
            Assert.Equal(0f, cmd.Collective);
            Assert.Equal(0, cmd.FlightModeSwitchPosition);
        }

        [Fact]
        public void Erzeugtes_Kommando_Serialisiert_Mit_Thrust_Throttle_Spiegel()
        {
            // Integration: Mapping -> Payload; Thrust-Fenster == Throttle-Fenster
            var cmd = ControllerMapping.ToManualControlCommand(0, 16384, 0, 0, true);
            byte[] payload = cmd.ToPayload();
            string throttle = BitConverter.ToString(payload, 0, 4);
            string thrust = BitConverter.ToString(payload, 20, 4);
            Assert.Equal(thrust, throttle);
            // Throttle(16384) = 49152/65535 ~ 0.7500114 — im Rahmen 0.74..0.76
            Assert.InRange(cmd.Throttle, 0.74f, 0.76f);
        }
    }
}
