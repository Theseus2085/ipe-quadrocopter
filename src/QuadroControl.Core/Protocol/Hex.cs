using System;
using System.Collections.Generic;
using System.Text;

namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// Hilfsfunktionen für Hex-Strings der Form "XX XX XX ..." (Bytes durch
    /// Leerzeichen getrennt), wie sie die Kurs-Bibliothek KTCQuadroControl
    /// erwartet (SchickeHexString). Keine CRC-Berechnung: die Checksumme
    /// hängt die DLL selbst an (siehe wissen-programmierung.md §5, Feld G).
    /// </summary>
    public static class Hex
    {
        /// <summary>
        /// Wandelt ein Byte-Array in einen Hex-String "XX XX XX ..." um
        /// (Großbuchstaben, ein Leerzeichen zwischen den Bytes).
        /// </summary>
        public static string ToHexString(byte[] bytes)
        {
            if (bytes == null) throw new ArgumentNullException("bytes");
            var sb = new StringBuilder(bytes.Length * 3);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(bytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Parst einen Hex-String "XX XX ..." zurück in ein Byte-Array.
        /// Tolerant gegenüber Groß-/Kleinschreibung und mehreren Leerzeichen,
        /// damit auch die wortwörtlich aus VL5 übernommenen (kleingeschriebenen)
        /// Metadaten-Nachrichten geprüft werden können.
        /// </summary>
        public static byte[] Parse(string hexString)
        {
            if (hexString == null) throw new ArgumentNullException("hexString");
            var tokens = hexString.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new byte[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                result[i] = Convert.ToByte(tokens[i], 16);
            }
            return result;
        }

        /// <summary>
        /// Prüft, ob zwei Hex-Strings dieselbe Bytefolge beschreiben
        /// (unabhängig von Groß-/Kleinschreibung und Whitespace).
        /// </summary>
        public static bool SameBytes(string hexA, string hexB)
        {
            byte[] a = Parse(hexA);
            byte[] b = Parse(hexB);
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
    }
}
