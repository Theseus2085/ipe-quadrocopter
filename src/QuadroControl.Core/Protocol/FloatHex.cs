using System;

namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// Umrechnung float / int16 in Little-Endian-Hex-Strings "XX XX XX XX".
    /// Bit-genaueres Gegenstück zu KonvertiereFloatZuHexString() der
    /// KTCQuadroControl-Bibliothek (wissen-programmierung.md §5):
    /// IEEE-754-Bits, niederwertigstes Byte zuerst (Little Endian),
    /// da UAVTalk alle Felder little-endian überträgt.
    /// </summary>
    public static class FloatHex
    {
        /// <summary>
        /// float32 → Hex-String "XX XX XX XX" (Little Endian).
        /// Beispiel: 1.0f → "00 00 80 3F", -1.0f → "00 00 80 BF".
        /// </summary>
        public static string ToHexString(float value)
        {
            return Hex.ToHexString(ToBytes(value));
        }

        /// <summary>
        /// float32 → 4 Bytes Little Endian (IEEE 754).
        /// Endian-Sicherheit: BitConverter.GetBytes liefert nur auf
        /// Little-Endian-Systemen direkt die UAVTalk-Reihenfolge.
        /// </summary>
        public static byte[] ToBytes(float value)
        {
            // SingleToInt32Bits gibt es erst in neueren Runtimes -> sicherer
            // Weg über BitConverter mit expliziter Endian-Korrektur.
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// 4 Bytes Little Endian → float32 (Umkehrung von ToBytes).
        /// </summary>
        public static float FromBytes(byte[] littleEndianBytes)
        {
            if (littleEndianBytes == null) throw new ArgumentNullException("littleEndianBytes");
            if (littleEndianBytes.Length != 4)
                throw new ArgumentException("float32 benötigt genau 4 Bytes (IEEE 754).", "littleEndianBytes");

            byte[] tmp = new byte[4];
            Array.Copy(littleEndianBytes, tmp, 4);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(tmp);
            }
            return BitConverter.ToSingle(tmp, 0);
        }

        /// <summary>
        /// int16 → Hex-String "XX XX" (Little Endian), z. B. für die
        /// Channel-int16[10]-Felder des ManualControlCommand.
        /// </summary>
        public static string Int16ToHexString(short value)
        {
            return Hex.ToHexString(Int16ToBytes(value));
        }

        /// <summary>
        /// int16 → 2 Bytes Little Endian.
        /// </summary>
        public static byte[] Int16ToBytes(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }
    }
}
