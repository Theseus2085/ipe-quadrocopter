namespace QuadroControl.Core.Communication
{
    /// <summary>
    /// Transport-Schnittstelle zwischen Anwendungslogik und den Kurs-DLLs.
    /// Zwei Implementierungen (je eine pro Programm):
    ///   QuadroControl.Virtuell: VirtuellQuadroTransport → vKTCQuadroControl
    ///     (vVerbinde/vVerbindungTrennen/vSchickeHexString über das
    ///     vorgegebene Delegat, Block "NICHT ÄNDERN")
    ///   QuadroControl.Real: RealQuadroTransport → KTCQuadroControl
    ///     (Verbinde/VerbindungTrennen/statische SchickeHexString)
    /// Die CRC-8-Checksummme hängt in BEIDEN Fällen die DLL an.
    /// </summary>
    public interface IQuadroTransport
    {
        /// <summary>Baut die Verbindung zum (virtuellen) COM-Port auf. true bei Erfolg.</summary>
        bool Verbinde(string comPort);

        /// <summary>Trennt die Verbindung. true bei Erfolg.</summary>
        bool VerbindungTrennen();

        /// <summary>
        /// Sendet einen UAVTalk-Hex-String "XX XX ..." (ohne Checksumme —
        /// die wird automatisch berechnet und angehängt).
        /// </summary>
        void SchickeHexString(string hexString);
    }
}
