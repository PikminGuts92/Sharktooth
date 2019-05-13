using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

/*
 * HEADER (16 bytes)
 * =================
 * INT32 - Version (1 or 2)
 * INT32 - Hash
 * INT32 - Count of entries
 * INT32 - Size of string blob
 * Entries[]
 * StringBlob
 * 
 * Entry (16 bytes)
 * ================
 * FLOAT - Start (0-index)
 * INT32 - Pitch / Identifier
 *  [0] Type
 *   0x00 - Note
 *   0x01
 *   0x02 ^^
 *   0x03 ^
 *   0x04 ^^
 *   0x05
 *   0x09 - Section ^^
 *   0x0A - Author ^^
 *   0x0C
 *   0x0D
 *     ^ = Sometimes FF'd
 *    ^^ = FF'd
 *  [1]
 *  [2]
 *  [3] Pitch
 * FLOAT - Length
 * INT32 - Text Pointer (Usually 0)
 * 	Pointer to text value in blob, starting from first entry
 *
 * 0x0A
 *  <PlayAnim on="Peripheral2" anim="p_punch_air_01" start="0" />
 *  <StopAnim on="Peripheral2"
 */

namespace Sharktooth.Mub
{
    public class Mub
    {
        public Mub()
        {
            Version = 1;
            Entries = new List<MubEntry>();
        }

        public static Mub FromFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return FromStream(fs);
            }
        }

        private static Mub FromStream(Stream stream)
        {
            AwesomeReader ar = new AwesomeReader(stream, true);
            Mub mub = new Mub();

            mub.Version = ar.ReadInt32();
            ar.BaseStream.Position += 4; // CRC-32 hash

            int entryCount = ar.ReadInt32(), entrySize = entryCount * 16;
            int blobSize = ar.ReadInt32();
            long startOffset = ar.BaseStream.Position;

            // Reads in strings
            ar.BaseStream.Seek(entrySize, SeekOrigin.Current);
            var stringBlob = ar.ReadBytes(blobSize);
            Dictionary<long, string> words = ParseBlob(stringBlob, entrySize);
            ar.BaseStream.Seek(startOffset, SeekOrigin.Begin);

            // Reads entries
            for (int i = 0; i < entryCount; i++)
            {
                float start = ar.ReadSingle();
                int mod = ar.ReadInt32();
                float length = ar.ReadSingle();
                int wordOffset = ar.ReadInt32();

                mub.Entries.Add(new MubEntry(start, mod, length, wordOffset > 0 && words.ContainsKey(wordOffset) ? words[wordOffset] : ""));
            }
            
            return mub;
        }

        public void ToStream(Stream stream)
        {
            long startOffset;
            int size;

            var data = CreateData();
            // TODO: Calculate new hash from data

            var aw = new AwesomeWriter(stream, true);
            startOffset = stream.Position;

            aw.Write((int)Version);
            aw.Write((int)0); // CRC-32 hash

            aw.Write(data);
            size = (int)(stream.Position - startOffset);
            if (size % 4 != 0)
            {
                // Write byte difference
                var remain = new byte[4 - (size % 4)];
                aw.Write(remain);
            }
        }

        private byte[] CreateData()
        {
            var stringBlob = CreateStringBlob(out var stringOffsets);
            
            using (var ms = new MemoryStream())
            {
                using (var aw = new AwesomeWriter(ms, true))
                {
                    // Writes entry count + blob size
                    aw.Write((int)Entries.Count);
                    aw.Write((int)stringBlob.Length);

                    // Writes entries
                    foreach (var entry in Entries)
                    {
                        aw.Write((float)entry.Start);
                        aw.Write((int)entry.Modifier);
                        aw.Write((float)entry.Length);
                        aw.Write(stringOffsets.ContainsKey(entry.Text) ? stringOffsets[entry.Text] : 0);
                    }

                    // Writes string blob
                    aw.Write(stringBlob);
                }

                return ms.ToArray();
            }
        }

        private byte[] CreateStringBlob(out Dictionary<string, int> offsets)
        {
            var strings = Entries
                .Select(x => x.Text)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            int idx = 0;
            byte[] data;

            offsets = new Dictionary<string, int>();

            using (var ms = new MemoryStream())
            {
                foreach (var str in strings)
                {
                    data = Encoding.UTF8.GetBytes(str);
                    ms.Write(data, 0, data.Length);
                    ms.WriteByte(0x00);

                    offsets.Add(str, idx);
                    idx += data.Length + 1;
                }

                return ms.ToArray();
            }
        }

        private static Dictionary<long, string> ParseBlob(byte[] blob, long offset)
        {
            Dictionary<long, string> words = new Dictionary<long, string>();

            int i = 0;
            while (i < blob.Length)
            {
                int size = Array.IndexOf(blob, byte.MinValue, i);
                if (size == -1) break;
                size -= i;

                words.Add(i + offset, Encoding.UTF8.GetString(blob, i, size));
                i += size + 1;
            }

            return words;
        }

        public int Version { get; set; }
        public List<MubEntry> Entries { get; set; }
    }
}
