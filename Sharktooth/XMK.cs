﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Sharktooth
{
    public class XMK
    {
        public XMK()
        {
            Version = 8;

            TempoEntries = new List<XMKTempo>();
            Entries = new List<XMKEvent>();
        }

        public static XMK FromFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return FromStream(fs);
            }
        }

        public static XMK FromStream(Stream stream)
        {
            AwesomeReader ar = new AwesomeReader(stream, true);
            XMK xmk = new XMK();

            // Reads header info
            xmk.Version = ar.ReadInt32();
            xmk.Hash = ar.ReadInt32();
            
            int entryCount = ar.ReadInt32();
            int blobSize = ar.ReadInt32();
            xmk.Unknown1 = ar.ReadUInt32();

            int tempoCount = ar.ReadInt32();
            int tsCount = ar.ReadInt32(); // Still unsure about that one
            xmk.Unknown2 = ar.ReadUInt32();

            // Parses tempo map
            for (int i = 0; i < tempoCount; i++)
            {
                XMKTempo entry = new XMKTempo()
                {
                    Start = ar.ReadSingle(),
                    MicroPerQuarter = ar.ReadUInt32(),
                    Ticks = ar.ReadUInt32()
                };
                xmk.TempoEntries.Add(entry);
            }

            // Skips unknown data
            ar.BaseStream.Position += (16 * tsCount) - 4;
            long startOffset = ar.BaseStream.Position;

            // Reads in strings
            ar.BaseStream.Seek(entryCount * 24, SeekOrigin.Current);
            xmk.StringBlob = ar.ReadBytes(blobSize);
            Dictionary<long, string> words = ParseBlob(xmk.StringBlob);
            ar.BaseStream.Seek(startOffset, SeekOrigin.Begin);

            // Parses events
            for (int i = 0; i < entryCount; i++)
            {
                XMKEvent entry = new XMKEvent()
                {
                    Unknown1 = ar.ReadUInt32(),
                    Unknown2 = ar.ReadUInt16(),
                    Unknown3 = ar.ReadByte(),
                    Pitch = ar.ReadByte(),
                    Start = ar.ReadSingle(),
                    End = ar.ReadSingle(),
                    Unknown4 = ar.ReadUInt32()
                };

                // Adds text
                int offset = ar.ReadInt32() - (entryCount * 24);
                if (offset >= 0)
                    entry.Text = words[offset];

                xmk.Entries.Add(entry);
            }

            return xmk;
        }

        private static Dictionary<long, string> ParseBlob(byte[] blob)
        {
            Dictionary<long, string> words = new Dictionary<long, string>();

            int i = 0;
            while (i < blob.Length)
            {
                int size = Array.IndexOf(blob, byte.MinValue, i);
                if (size == -1) break;
                size -= i;

                words.Add(i, Encoding.UTF8.GetString(blob, i, size));
                i += size + 1;
            }

            return words;
        }

        public int Version { get; set; }
        public int Hash { get; set; }
        public uint Unknown1 { get; set; }
        public uint Unknown2 { get; set; }

        public List<XMKTempo> TempoEntries { get; set; }
        public List<XMKEvent> Entries { get; set; }
        public byte[] StringBlob { get; set; }
    }
}
