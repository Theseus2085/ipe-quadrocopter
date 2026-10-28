using QuadroControl.Core.Controller;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests der Controller-Belegung (Mode-2, Entscheidung D3):
    /// linker Stick Y → Throttle 0…1, linker Stick X → Yaw,
    /// rechter Stick X → Roll, rechter Stick Y → Pitch (je -1…1).
    /// </summary>
    public class ControllerMappingTests
    {
        [Fact]
        public void Throttle_Unten_Minus32768_Ist_0()
        {
            Assert.Equal(0f, ControllerMapping.ThrottleFromAxis(-32768), 5);
        }

        [Fact]
        public void Throttle_Oben_32767_Ist_1()
        {
            Assert.Equal(1f, ControllerMapping.ThrottleFromAxis(32767), 5);
        }

        [Fact]
        public void Throttle_Mitte_0_Ist_Ca_0_5()
        {
            Assert.Equal(0.5f, ControllerMapping.ThrottleFromAxis(0), 3);
        }

        [Theory]
        [InlineData(32767)]
        [InlineData(-32767)]
        [InlineData(0)]
        [InlineData(12345)]
        public void Throttle_Bleibt_Im_Bereich_0_bis_1(short achse)
        {
            float wert = ControllerMapping.ThrottleFromAxis(achse);
            Assert.InRange(wert, 0f, 1f);
        }

        [Theory]
        [InlineData(32767)]
        [InlineData(-32767)]
        [InlineData(0)]
        [InlineData(-12345)]
        public void Normierte_Achse_Bleibt_Im_Bereich_Minus1_bis_1(short achse)
        {
            float wert = ControllerMapping.AxisToNormalized(achse);
            Assert.InRange(wert, -1f, 1f);
        }

        [Fact]
        public void Achse_32767_Ist_Genau_1()
        {
            Assert.Equal(1f, ControllerMapping.AxisToNormalized(32767), 5);
        }

        [Fact]
        public void ToManualControlCommand_Liefert_Vollstaendiges_Objekt()
        {
            var cmd = ControllerMapping.ToManualControlCommand(
                leftStickX: -16384, leftStickY: 16384,
                rightStickX: 32767, rightStickY: -32767,
                controllerConnected: true);

            // linker Stick X = Yaw, rechter X = Roll, rechter Y = Pitch, linker Y = Throttle
            Assert.Equal(-0.5f, cmd.Yaw, 3);
            Assert.Equal(1f, cmd.Roll, 3);
            Assert.Equal(-1f, cmd.Pitch, 3);
            Assert.InRange(cmd.Throttle, 0.74f, 0.76f); // (16384+32768)/65535 ≈ 0.75
            Assert.Equal(0f, cmd.Collective);
            Assert.Equal(0, cmd.FlightModeSwitchPosition);
            Assert.Equal(1, cmd.Connected);
        }

        [Fact]
        public void ToManualControlCommand_Ohne_Controller_Connected_0()
        {
            var cmd = ControllerMapping.ToManualControlCommand(0, 0, 0, 0, controllerConnected: false);
            Assert.Equal(0, cmd.Connected);
        }
    }
}
