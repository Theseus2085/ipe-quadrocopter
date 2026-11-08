using QuadroControl.Core.Controller;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests der Controller-Belegung (Entscheidung D3v2):
    /// R2 (rechter Trigger) → Throttle 0…1, linker Stick X → Yaw,
    /// rechter Stick X → Roll, rechter Stick Y → Pitch (je -1…1).
    /// </summary>
    public class ControllerMappingTests
    {
        [Fact]
        public void Throttle_Ruhe_0_Ist_0()
        {
            Assert.Equal(0f, ControllerMapping.ThrottleFromTrigger(0), 5);
        }

        [Fact]
        public void Throttle_Totzonenrand_10_Ist_0()
        {
            Assert.Equal(0f, ControllerMapping.ThrottleFromTrigger(10), 5);
        }

        [Fact]
        public void Throttle_Erster_Wirksamer_Wert_11_Ist_Knapp_Ueber_0()
        {
            // (11-10)/(255-10) = 1/245 ≈ 0.004
            Assert.InRange(ControllerMapping.ThrottleFromTrigger(11), 0.003f, 0.005f);
        }

        [Fact]
        public void Throttle_Voll_255_Ist_1()
        {
            Assert.Equal(1f, ControllerMapping.ThrottleFromTrigger(255), 5);
        }

        [Fact]
        public void Throttle_Mitte_133_Ist_Ca_0_5()
        {
            // (133-10)/245 = 123/245 ≈ 0.502
            Assert.Equal(0.5f, ControllerMapping.ThrottleFromTrigger(133), 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(200)]
        [InlineData(255)]
        public void Throttle_Bleibt_Im_Bereich_0_bis_1(byte trigger)
        {
            float wert = ControllerMapping.ThrottleFromTrigger(trigger);
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
                leftStickX: -16384,
                rightStickX: 32767, rightStickY: -32767,
                rightTrigger: 191,
                controllerConnected: true);

            // linker Stick X = Yaw, rechter X = Roll, rechter Y = Pitch, R2 = Throttle
            Assert.Equal(-0.5f, cmd.Yaw, 3);
            Assert.Equal(1f, cmd.Roll, 3);
            Assert.Equal(-1f, cmd.Pitch, 3);
            Assert.InRange(cmd.Throttle, 0.73f, 0.75f); // (191-10)/245 ≈ 0.739
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
