using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

namespace Sharktooth.Mub
{
    public class MubExport
    {
        private const int DELTA_TICKS_PER_QUARTER = 480;
        private Mub _mub;

        public MubExport(Mub mub)
        {
            _mub = mub;
        }

        public void Export(string path)
        {
            MidiEventCollection mid = new MidiEventCollection(1, DELTA_TICKS_PER_QUARTER);
            mid.AddTrack(CreateTempoTrack());
            mid.AddTrack(CreateTrack());

            MidiFile.Export(path, mid);
        }

        private List<MidiEvent> CreateTempoTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("mubTempo", MetaEventType.SequenceTrackName, 0));
            
            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> CreateTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("NOTES", MetaEventType.SequenceTrackName, 0));

            int ticksPerMeasure = DELTA_TICKS_PER_QUARTER * 4; // Assume 4/4
            foreach (var entry in _mub.Entries)
            {
                long start = (long)(entry.Start * ticksPerMeasure);
                long end = (long)(start + (entry.Length * ticksPerMeasure));

                if ((entry.Modifier & 0xFFFFFF) == 0xFFFFFF)
                {
                    // Text event?
                    if (!string.IsNullOrEmpty(entry.Text))
                        track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));
                    continue;
                }

                if (entry.Length <= 0) continue;
                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, entry.Modifier & 0xFF, 100));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, entry.Modifier & 0xFF, 100));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }
    }
}
