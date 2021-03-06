﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Sharktooth.Xmk
{
    public class Xmk
    {
        private string _filePath;

        public Xmk()
        {
            Version = 8;

            TempoEntries = new List<XmkTempo>();
            TimeSignatureEntries = new List<XmkTimeSignature>();
            Entries = new List<XmkEvent>();
        }

        public static Xmk FromFile(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                return FromStream(fs, path);
            }
        }

        public static Xmk FromStream(Stream stream, string filePath = "")
        {
            AwesomeReader ar = new AwesomeReader(stream, true);
            Xmk xmk = new Xmk();
            xmk._filePath = filePath;

            // Reads header info
            xmk.Version = ar.ReadInt32();
            xmk.Hash = ar.ReadInt32();

            int entryCount = ar.ReadInt32();
            int blobSize = ar.ReadInt32();
            xmk.Unknown1 = ar.ReadUInt32();

            int tempoCount = ar.ReadInt32();
            int tsCount = ar.ReadInt32();

            // Parses tempo map
            for (int i = 0; i < tempoCount; i++)
            {
                XmkTempo entry = new XmkTempo()
                {
                    Ticks = ar.ReadUInt32(),
                    Start = ar.ReadSingle(),
                    MicroPerQuarter = ar.ReadUInt32()
                };
                xmk.TempoEntries.Add(entry);
            }

            // Parse time signatures
            for (int i = 0; i < tsCount; i++)
            {
                XmkTimeSignature ts = new XmkTimeSignature()
                {
                    Ticks = ar.ReadUInt32(),
                    Measure = ar.ReadInt32(),
                    Numerator = ar.ReadInt32(),
                    Denominator = ar.ReadInt32()
                };

                xmk.TimeSignatureEntries.Add(ts);
            }

            var stringOffset = entryCount * xmk.GetEntrySize();

            // Reads in strings
            long startOffset = ar.BaseStream.Position;
            ar.BaseStream.Seek(stringOffset, SeekOrigin.Current);
            xmk.StringBlob = ar.ReadBytes(blobSize);
            Dictionary<long, string> words = ParseBlob(xmk.StringBlob);
            ar.BaseStream.Seek(startOffset, SeekOrigin.Begin);

            // Parses events
            if (xmk.Version == 5) // Sing Party
            {
                for (int i = 0; i < entryCount; i++)
                {
                    // 16 bytes
                    XmkEvent entry = new XmkEvent()
                    {
                        Unknown1 = 0,
                        Unknown2 = ar.ReadUInt16(),
                        Unknown3 = ar.ReadByte(),
                        Pitch = ar.ReadByte(),
                        Start = ar.ReadSingle(),
                        End = ar.ReadSingle(),
                        Unknown4 = 0
                    };

                    // Adds text
                    int offset = ar.ReadInt32() - stringOffset;
                    if (offset >= 0 && words.ContainsKey(offset))
                        entry.Text = words[offset];

                    xmk.Entries.Add(entry);
                }
            }
            else // GHL
            {
                for (int i = 0; i < entryCount; i++)
                {
                    // 24 bytes
                    XmkEvent entry = new XmkEvent()
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
                    int offset = ar.ReadInt32() - stringOffset;
                    if (offset >= 0 && words.ContainsKey(offset))
                        entry.Text = words[offset];

                    xmk.Entries.Add(entry);
                }
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

        private int GetEntrySize()
            => Version == 5
                ? 16  // Sing Party
                : 24; // GHL

        public string Name => Path.GetFileNameWithoutExtension(_filePath);
        public int Version { get; set; }
        public int Hash { get; set; }
        public uint Unknown1 { get; set; }

        public List<XmkTempo> TempoEntries { get; set; }
        public List<XmkTimeSignature> TimeSignatureEntries { get; set; }
        public List<XmkEvent> Entries { get; set; }
        public byte[] StringBlob { get; set; }
    }
}
