using System;

namespace QuadroControl.Core.Protocol
{
    /// <summary>
    /// Baut UAVTalk-Nachrichten als Hex-String "XX XX ..." zusammen
    /// (wissen-programmierung.md §5, Nachrichtenaufbau Feld A–F):
    ///   A Sync   1 Byte  immer 3C
    ///   B Type   1 Byte  (UavTalkMessageType)
    ///   C Length 2 Byte  int16 Little Endian = Anzahl Bytes Header+Daten OHNE Checksumme
    ///   D ObjID  4 Byte  Tabellen-Reihenfolge byte-verdreht (Little Endian)
    ///   E Instanz 2 Byte meist 00 00
    ///   F Daten  X Byte  alle Variablen des UAVObjects in fester Reihenfolge
    ///   G CRC    —       wird NICHT angehängt! SchickeHexString() der Kurs-DLL
    ///                    berechnet die CRC-8 und hängt sie selbst an.
    /// </summary>
    public static class UavTalkBuilder
    {
        /// <summary>Sync-Value jedes UAVTalk-Rahmens (Feld A).</summary>
        public const byte SyncByte = 0x3C;

        /// <summary>Länge des UAVTalk-Headers in Bytes (Sync, Type, Length, ObjectID, InstanceID).</summary>
        public const int HeaderLength = 10;

        /// <summary>
        /// Baut eine komplette UAVTalk-Nachricht (ohne Checksumme) als Byte-Array.
        /// </summary>
        /// <param name="messageType">Message-Type (Feld B), i. d. R. ObjectData (0x20).</param>
        /// <param name="objectId">ObjectID in Tabellen-Reihenfolge (Feld D), 4 Bytes.</param>
        /// <param name="instanceId">InstanceID (Feld E), 2 Bytes, oder null für 00 00.</param>
        /// <param name="data">Datenabschnitt (Feld F) mit allen Variablen des UAVObjects.</param>
        public static byte[] Build(UavTalkMessageType messageType, byte[] objectId, byte[] instanceId, byte[] data)
        {
            if (objectId == null) throw new ArgumentNullException("objectId");
            if (objectId.Length != 4) throw new ArgumentException("ObjectID muss genau 4 Bytes lang sein.", "objectId");
            if (data == null) data = new byte[0];

            int totalLength = HeaderLength + data.Length; // Length-Feld: Header+Daten OHNE Checksumme
            if (totalLength > short.MaxValue)
                throw new ArgumentException("Nachricht länger als int16-Length-Feld erlaubt.");

            byte[] message = new byte[totalLength];

            // A: Sync-Value
            message[0] = SyncByte;

            // B: Message-Type
            message[1] = (byte)messageType;

            // C: Length, Little Endian (niederwertiges Byte zuerst)
            message[2] = (byte)(totalLength & 0xFF);
            message[3] = (byte)((totalLength >> 8) & 0xFF);

            // D: ObjectID, Little Endian = Tabellen-Reihenfolge umgedreht
            message[4] = objectId[3];
            message[5] = objectId[2];
            message[6] = objectId[1];
            message[7] = objectId[0];

            // E: InstanceID, Little Endian, meist 00 00
            if (instanceId == null || instanceId.Length == 0)
            {
                message[8] = 0x00;
                message[9] = 0x00;
            }
            else
            {
                if (instanceId.Length != 2) throw new ArgumentException("InstanceID muss 0 oder 2 Bytes lang sein.", "instanceId");
                message[8] = instanceId[1];
                message[9] = instanceId[0];
            }

            // F: Datenabschnitt
            Array.Copy(data, 0, message, HeaderLength, data.Length);

            // G: KEINE Checksumme — hängt SchickeHexString() der Kurs-DLL automatisch an.
            return message;
        }

        /// <summary>
        /// Baut eine komplette UAVTalk-Nachricht (ohne Checksumme) als
        /// Hex-String "XX XX ...", direkt sendbar an SchickeHexString/vSchickeHexString.
        /// </summary>
        public static string BuildHex(UavTalkMessageType messageType, byte[] objectId, byte[] instanceId, byte[] data)
        {
            return Hex.ToHexString(Build(messageType, objectId, instanceId, data));
        }
    }
}
