using Xunit;

namespace QuadroControl.Core.Tests
{
    /// <summary>
    /// Platzhalter-Test aus Phase P0: beweist, dass das Testprojekt auf der
    /// Linux-VM gebaut und ausgeführt werden kann (dotnet test).
    /// Die fachlichen Tests folgen in Phase P1.
    /// </summary>
    public class P0SmokeTests
    {
        [Fact]
        public void Testprojekt_Ist_Ausfuehrbar()
        {
            Assert.True(true);
        }
    }
}
