using System;
using QuadroControl.Core.Communication;
using Kqc = KTCQuadroControl.QuadroControl;

namespace QuadroControl.Real
{
    /// <summary>
    /// Transport-Adapter für den realen Quadrocopter (Programm 2).
    /// Kapselt die vorgeschriebene KTCQuadroControl-Bibliothek (PROG-007):
    /// Verbinde / VerbindungTrennen / statische SchickeHexString —
    /// SchickeHexString berechnet die CRC-8-Checksumme automatisch und hängt
    /// sie an. KonvertiereFloatZuHexString und HoleAktuelleCOMPorts werden
    /// zusätzlich für die GUI bereitgestellt.
    /// Der Alias Kqc vermeidet Namenskonflikte zwischen der Klasse
    /// QuadroControl und dem Namensraum QuadroControl.Core.
    /// </summary>
    public sealed class RealQuadroTransport : IQuadroTransport
    {
        private readonly Kqc _myQC = new Kqc();

        /// <summary>COM-Port des FC verbinden ("COMx", z. B. Revolution Virtual COM Port bzw. OPLink).</summary>
        public bool Verbinde(string comPort)
        {
            return _myQC.Verbinde(comPort);
        }

        /// <summary>COM-Port schließen.</summary>
        public bool VerbindungTrennen()
        {
            return _myQC.VerbindungTrennen();
        }

        /// <summary>Sendet den Hex-String an den FC (Checksumme wird automatisch angehängt).</summary>
        public void SchickeHexString(string hexString)
        {
            Kqc.SchickeHexString(hexString); // statische Methode der Kurs-DLL
        }

        /// <summary>Aktuell belegte COM-Ports des PCs (nur in KTCQuadroControl enthalten; Instanzmethode, s. VL5).</summary>
        public string[] HoleAktuelleCOMPorts()
        {
            return _myQC.HoleAktuelleCOMPorts();
        }

        /// <summary>
        /// float → "XX XX XX XX" (bereits Little Endian) — Kurs-DLL-Funktion,
        /// hier für die Anzeige/Test der Wertekodierung in der GUI verwendet.
        /// </summary>
        public string KonvertiereFloatZuHexString(float floatWert)
        {
            return Kqc.KonvertiereFloatZuHexString(floatWert);
        }
    }
}
