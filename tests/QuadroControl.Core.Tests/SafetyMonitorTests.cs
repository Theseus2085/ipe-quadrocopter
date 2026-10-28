using QuadroControl.Core.Safety;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Tests der Sicherheitslogik (PROG-009A/011):
    /// Controller-Verlust im bewaffneten Zustand → automatisches Disarming +
    /// neutrale Steuerung; kein Eingang → neutraler ManualControlCommand.
    /// </summary>
    public class SafetyMonitorTests
    {
        private readonly SafetyMonitor _monitor = new SafetyMonitor();

        [Fact]
        public void Controller_Verlust_Bewaffnet_Loest_Disarming_Aus()
        {
            SafetyDecision d = _monitor.Evaluate(controllerConnected: false, isArmed: true, anyInputActive: false);
            Assert.True(d.ControllerLost);
            Assert.True(d.ShallDisarm);
            Assert.True(d.ShallSendNeutralCommand);
        }

        [Fact]
        public void Controller_Verlust_Unbewaffnet_Sendet_Neutral()
        {
            SafetyDecision d = _monitor.Evaluate(controllerConnected: false, isArmed: false, anyInputActive: false);
            Assert.True(d.ControllerLost);
            Assert.False(d.ShallDisarm);
            Assert.True(d.ShallSendNeutralCommand);
        }

        [Fact]
        public void Kein_Eingang_Sendet_Neutralen_Befehl()
        {
            SafetyDecision d = _monitor.Evaluate(controllerConnected: true, isArmed: true, anyInputActive: false);
            Assert.False(d.ControllerLost);
            Assert.False(d.ShallDisarm);
            Assert.True(d.ShallSendNeutralCommand);
        }

        [Fact]
        public void Aktive_Eingabe_Laesst_Steuersignale_Zu()
        {
            SafetyDecision d = _monitor.Evaluate(controllerConnected: true, isArmed: true, anyInputActive: true);
            Assert.False(d.ControllerLost);
            Assert.False(d.ShallDisarm);
            Assert.False(d.ShallSendNeutralCommand);
        }

        [Fact]
        public void NeutralCommand_Throttle_0_Allgemein_Stillstand()
        {
            var cmd = SafetyMonitor.NeutralCommand();
            Assert.Equal(0f, cmd.Throttle);
            Assert.Equal(0f, cmd.Roll);
            Assert.Equal(0f, cmd.Pitch);
            Assert.Equal(0f, cmd.Yaw);
            Assert.Equal(0f, cmd.Thrust);
            Assert.Equal(0, cmd.Connected);
        }
    }
}
