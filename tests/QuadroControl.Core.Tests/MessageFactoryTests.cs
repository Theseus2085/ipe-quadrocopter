using QuadroControl.Core.Communication;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Konsistenz-Tests der MessageFactory: Heartbeat identisch zur dritten
    /// Handshake-Nachricht, Arming-/Disarming-Werte, Konfigurationswerte.
    /// </summary>
    public class MessageFactoryTests
    {
        [Fact]
        public void Heartbeat_Ist_Identisch_Mit_Dritter_Handshake_Nachricht()
        {
            string[] handshake = HandshakeSequenceProvider.GetMessages();
            Assert.Equal(handshake[2], MessageFactory.BuildHeartbeat());
        }

        [Theory]
        [InlineData(1f, "3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 3F")]
        [InlineData(-1f, "3C 20 0E 00 5A 98 09 C4 00 00 00 00 80 BF")]
        public void AccessoryDesired_Arming_Und_Disarming_Vektoren(float wert, string erwartet)
        {
            Assert.Equal(erwartet, MessageFactory.BuildAccessoryDesired(wert));
        }

        [Fact]
        public void Konfiguration_Heartbeat_Eine_Sekunde_Arming_Mehrfach()
        {
            Assert.Equal(1000, QuadroConfig.HeartbeatIntervalMs);
            Assert.True(QuadroConfig.ArmingRepeatCount >= 2, "Arming muss MCC mit Throttle=-1 mehrfach schreiben (VL5).");
            Assert.Equal(1f, QuadroConfig.AccessoryArmingValue);
            Assert.Equal(-1f, QuadroConfig.AccessoryDisarmingValue);
        }
    }
}
