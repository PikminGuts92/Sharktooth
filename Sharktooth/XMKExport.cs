using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

namespace Sharktooth
{
    public class XMKExport
    {
        private const int DELTA_TICKS_PER_QUARTER = 480;
        private readonly List<TempoIndex> _tempoIdx = new List<TempoIndex>();
        private XMK _xmk;

        public XMKExport(XMK xmk)
        {
            _xmk = xmk;
        }

        public void Export(string path)
        {
            MidiEventCollection mid = new MidiEventCollection(1, DELTA_TICKS_PER_QUARTER);
            mid.AddTrack(CreateTempoTrack(_xmk.TempoEntries));
            mid.AddTrack(CreateTrack());

            MidiFile.Export(path, mid);
        }

        private List<MidiEvent> CreateTempoTrack(List<XMKTempo> tempos)
        {
            List<MidiEvent> track = new List<MidiEvent>();
            _tempoIdx.Clear();
            track.Add(new NAudio.Midi.TextEvent("xmkTempo", MetaEventType.SequenceTrackName, 0));

            if (tempos.Count <= 0 || tempos[0].Start > 0.0f)
            {
                var idxEntry = new TempoIndex(0, 0, 120);
                //track.Add(new NAudio.Midi.TempoEvent(idxEntry.MicroPerQuarter, idxEntry.AbsoluteTime));
                _tempoIdx.Add(idxEntry);
            }

            long GetAbsoluteTime(double startTime, TempoIndex currentTempo)
            {
                double difference = startTime - currentTempo.RealTime;
                long absoluteTicks = currentTempo.AbsoluteTime + (1000L * (long)difference * DELTA_TICKS_PER_QUARTER) / currentTempo.MicroPerQuarter;

                // Applies quantization and snaps to grid
                int q = DELTA_TICKS_PER_QUARTER / 32; // 1/128th quantization
                if (absoluteTicks % q != 0)
                {
                    long before = absoluteTicks % q;
                    long after = q - before;

                    if (before < after)
                        absoluteTicks -= before;
                    else
                        absoluteTicks += after;
                }

                return absoluteTicks;
            }

            // Adds tempo changes
            if (tempos.Count > 0)
            {
                var firstTempo = tempos.First();
                var idxEntry = new TempoIndex()
                {
                    AbsoluteTime = _tempoIdx.Count > 0 ? GetAbsoluteTime(firstTempo.Start * 1000, _tempoIdx.Last()) : 0,
                    RealTime = firstTempo.Start * 1000,
                    BPM = firstTempo.BPM
                };

                track.Add(new NAudio.Midi.TempoEvent(idxEntry.MicroPerQuarter, idxEntry.AbsoluteTime));
                _tempoIdx.Add(idxEntry);

                foreach (var tempoEntry in tempos.Skip(1))
                {
                    idxEntry = new TempoIndex()
                    {
                        AbsoluteTime = GetAbsoluteTime(tempoEntry.Start * 1000, _tempoIdx.Last()),
                        RealTime = tempoEntry.Start * 1000,
                        BPM = tempoEntry.BPM
                    };

                    track.Add(new NAudio.Midi.TempoEvent(idxEntry.MicroPerQuarter, idxEntry.AbsoluteTime));
                    _tempoIdx.Add(idxEntry);
                }
            }
            
            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private long GetAbsoluteTime(double startTime)
        {
            TempoIndex currentTempo = _tempoIdx.First();

            // Finds last tempo change before event
            foreach (TempoIndex idx in _tempoIdx.Skip(1))
            {
                if (idx.RealTime <= startTime) currentTempo = idx;
                else break;
            }

            double difference = startTime - currentTempo.RealTime;
            long absoluteTicks = currentTempo.AbsoluteTime + (1000L * (long)difference * DELTA_TICKS_PER_QUARTER) / currentTempo.MicroPerQuarter;

            // Applies quantization and snaps to grid
            int q = DELTA_TICKS_PER_QUARTER / 32; // 1/128th quantization
            if (absoluteTicks % q != 0)
            {
                long before = absoluteTicks % q;
                long after = q - before;

                if (before < after)
                    absoluteTicks -= before;
                else
                    absoluteTicks += after;
            }

            return absoluteTicks;
        }

        private List<MidiEvent> CreateTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("NOTES", MetaEventType.SequenceTrackName, 0));
            
            foreach (var entry in _xmk.Entries)
            {
                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                
                // Text event?
                if (!string.IsNullOrEmpty(entry.Text))
                    track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));

                if ((end - start) <= 0 || entry.Pitch > 127) continue;
                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, entry.Pitch, 100));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, entry.Pitch, 100));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }
    }
}
