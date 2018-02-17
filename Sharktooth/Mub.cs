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
 * FLOAT - Length
 * INT32 - Text Pointer (Usually 0)
 * 	Pointer to text value in blob, starting from first entry
 */

namespace Sharktooth
{
    public class Mub
    {
        public Mub()
        {
            Version = 1;
            Entries = new List<MubEntry>();
            StringBlob = null;
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
            mub.Hash = ar.ReadInt32();

            int entryCount = ar.ReadInt32(), entrySize = entryCount * 16;
            int blobSize = ar.ReadInt32();
            long startOffset = ar.BaseStream.Position;

            // Reads in strings
            ar.BaseStream.Seek(entrySize, SeekOrigin.Current);
            mub.StringBlob = ar.ReadBytes(blobSize);
            Dictionary<long, string> words = ParseBlob(mub.StringBlob, entrySize);
            ar.BaseStream.Seek(startOffset, SeekOrigin.Begin);

            // Reads entries
            for (int i = 0; i < entryCount; i++)
            {
                float start = ar.ReadSingle();
                int mod = ar.ReadInt32();
                float length = ar.ReadSingle();
                int wordOffset = ar.ReadInt32();

                mub.Entries.Add(new MubEntry(start, mod, length, wordOffset > 0 ? words[wordOffset] : ""));
            }
            
            return mub;
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
        public int Hash { get; set; }
        public List<MubEntry> Entries { get; set; }
        public byte[] StringBlob { get; set; }
    }
}
