using QuadroControl.Core;
using QuadroControl.Core.Communication;
using QuadroControl.Core.Protocol;
using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// T9 (TEST-AUFTRAG): QuadroConfig-Freeze — die konfigurierten Werte als
    /// eingefrorene Vertragswerte. Jede (versehentliche) Aenderung laesst diese
    /// Tests rot werden. Vorgaben: E4 (Heartbeat 1/s), E5/VL5 (Arming mehrfach,
    /// Accessory 1/-1), controllerbelegung.md (5x, 50 ms).
    /// </summary>
    public class T9QuadroConfigFreezeTests
    {
        [Fact]
        public void HeartbeatIntervalMs_Ist_1000()
        {
            // E4: "GCSTelemetryStats Status=03 jede Sekunde"
            Assert.Equal(1000, QuadroConfig.HeartbeatIntervalMs);
        }

        [Fact]
        public void ArmingRepeatCount_Ist_5()
        {
            // E5/VL5: ManualControlCommand mit Throttle=-1 "mehrfach" schreiben;
            // Projektentscheidung (controllerbelegung.md): genau 5x
            Assert.Equal(5, QuadroConfig.ArmingRepeatCount);
        }

        [Fact]
        public void ArmingRepeatDelayMs_Ist_50()
        {
            // controllerbelegung.md: "Abstand 50 ms"
            Assert.Equal(50, QuadroConfig.ArmingRepeatDelayMs);
        }

        [Fact]
        public void ControllerPollIntervalMs_Ist_20()
        {
            // Freeze des aktuellen Werts (50 Hz Abfragetakt) gegen versehentliche Aenderung
            Assert.Equal(20, QuadroConfig.ControllerPollIntervalMs);
        }

        [Fact]
        public void AccessoryArmingValue_Ist_Plus1()
        {
            // E5: Arming dann AccessoryDesired = 1
            Assert.Equal(1f, QuadroConfig.AccessoryArmingValue);
        }

        [Fact]
        public void AccessoryDisarmingValue_Ist_Minus1()
        {
            // E5: Disarming AccessoryDesired = -1
            Assert.Equal(-1f, QuadroConfig.AccessoryDisarmingValue);
        }

        // ---------- Kreuz-Konsistenz (kein Drift zwischen Konstanten) ----------

        [Fact]
        public void HeartbeatMessage_IntervalMs_Und_QuadroConfig_HeartbeatIntervalMs_Sind_Identisch_1000()
        {
            // Zwei Stellen, ein Takt: duerfen nie auseinanderlaufen (E4)
            Assert.Equal(QuadroConfig.HeartbeatIntervalMs, HeartbeatMessage.IntervalMs);
            Assert.Equal(1000, HeartbeatMessage.IntervalMs);
        }

        [Fact]
        public void AccessoryWerte_In_QuadroConfig_Und_AccessoryDesiredData_Sind_Identisch()
        {
            Assert.Equal(AccessoryDesiredData.ArmingValue, QuadroConfig.AccessoryArmingValue);
            Assert.Equal(AccessoryDesiredData.DisarmingValue, QuadroConfig.AccessoryDisarmingValue);
        }

        [Fact]
        public void ArmingRepeatCount_Ist_Mindestens_2_Erfuellt_Mehrmals_Schreiben()
        {
            // E5 fordert "mehrfach hintereinander schreiben" — 5 erfuellt das;
            // der Freeze-Test sichert, dass niemand auf 1 zurueckstutzt.
            Assert.True(QuadroConfig.ArmingRepeatCount >= 2,
                "ArmingRepeatCount muss 'mehrfach' sein (>= 2), ist " + QuadroConfig.ArmingRepeatCount);
        }
    }
}
