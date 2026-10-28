using System;
using QuadroControl.Core.Communication;
using vKTCQuadroControl;

namespace QuadroControl.Virtuell
{
    /// <summary>
    /// Transport-Adapter für die virtuelle Testumgebung (Programm 1).
    /// Kapselt die vorgeschriebene vKTCQuadroControl-Bibliothek (PROG-007):
    /// vVerbinde / vVerbindungTrennen / Senden über das in der Vorlage
    /// vorgegebene, dispatcher-gesicherte Delegat vSchickeHexString.
    /// </summary>
    public sealed class VirtuellQuadroTransport : IQuadroTransport
    {
        private readonly vQuadroControl _vmyQC;

        /// <summary>
        /// Senden nur über die dispatcher-gesicherte Methode vSchickeHexString
        /// des MainWindow (Block "NICHT ÄNDERN" aus der Vorlage).
        /// </summary>
        private readonly Action<string> _sende;

        /// <summary>
        /// Erzeugt den Adapter. Beide Parameter stammen aus dem
        /// "NICHT ÄNDERN"-Block der Vorlage (vmyQC bzw. vSchickeHexString).
        /// </summary>
        public VirtuellQuadroTransport(vQuadroControl vmyQC, Action<string> sendeDelegate)
        {
            if (vmyQC == null) throw new ArgumentNullException("vmyQC");
            if (sendeDelegate == null) throw new ArgumentNullException("sendeDelegate");
            _vmyQC = vmyQC;
            _sende = sendeDelegate;
        }

        /// <summary>Virtuellen COM-Port verbinden (Name beliebig, z. B. "COM3").</summary>
        public bool Verbinde(string comPort)
        {
            return _vmyQC.vVerbinde(comPort);
        }

        /// <summary>Virtuelle Verbindung trennen (Rückgabe immer true).</summary>
        public bool VerbindungTrennen()
        {
            return _vmyQC.vVerbindungTrennen();
        }

        /// <summary>
        /// Sendet über das vorgegebene Delegat (threadsicher, da die Methode
        /// selbst auf den GUI-Dispatcher umleitet). Checksumme hängt die DLL an.
        /// </summary>
        public void SchickeHexString(string hexString)
        {
            _sende(hexString);
        }
    }
}
