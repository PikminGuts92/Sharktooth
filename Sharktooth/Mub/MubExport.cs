using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sharktooth.Mub
{
    public class MubExport
    {
        private const int DELTA_TICKS_PER_QUARTER = 480;
        private Mub _mub;
        private List<MubTempoMarker> tempoMarkers;

        public MubExport(Mub mub)
        {
            _mub = mub;
        }

        public void Export(string path)
        {
            MidiEventCollection mid = new MidiEventCollection(1, DELTA_TICKS_PER_QUARTER);
            mid.AddTrack(CreateTempoTrack());
            mid.AddTrack(CreateTrack());
            List<MidiEvent> effectsTrack = CreateEffectsTrack();
            if (effectsTrack != null)
                mid.AddTrack(effectsTrack);

            MidiFile.Export(path, mid);
        }

        private long NotePosToTicks(double pos, bool useTempoMarkers = true)
        {
            int ticksPerMeasure = DELTA_TICKS_PER_QUARTER * 4; // Assume 4/4

            if (useTempoMarkers && tempoMarkers != null)
            {
                for (int i = 0; i < tempoMarkers.Count; ++i)
                {
                    if (i == tempoMarkers.Count - 1 || tempoMarkers[i + 1].AbsolutePos > pos)
                    {
                        return (long)Math.Round(tempoMarkers[i].GetBeatPos(pos) * ticksPerMeasure);
                    }
                }
                throw new Exception("Error converting note position using tempo markers");
            }
            return (long)Math.Round(pos * ticksPerMeasure);
        }

        private List<MidiEvent> CreateTempoTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();

            float chartBPM = 0;
            int chartUsPerQuarterNote = 500000; // 120 BPM

            track.Add(new TimeSignatureEvent(0, 4, 2, 24, 8)); // 4/4 ts

            // get chart BPM
            foreach (var entry in _mub.Entries)
            {
                if (entry.Modifier == 0x0B000002)
                {
                    if (chartBPM > 0)
                    {
                        throw new Exception("Mub has more than one Chart BPM!");
                    }
                    chartBPM = BitConverter.ToSingle(BitConverter.GetBytes(entry.Data), 0);
                }
            }

            // without a chart BPM, can't do an accurate tempomap so don't even try without one.
            if (chartBPM > 0)
            {
                chartUsPerQuarterNote = (int)Math.Round(60000000 / chartBPM);

                foreach (var entry in _mub.Entries)
                {
                    if (entry.Modifier == 0x0B000001)
                    {
                        long start = NotePosToTicks(entry.Start, false);
                        track.Add(new TempoEvent(entry.Data, start));
                        if (tempoMarkers == null)
                        {
                            tempoMarkers = new List<MubTempoMarker>();
                        }
                        tempoMarkers.Add(new MubTempoMarker(entry.Start, entry.Data, chartUsPerQuarterNote));
                    }
                }
                if (tempoMarkers != null)
                {
                    tempoMarkers.Sort((x, y) =>
                    {
                        if (x.BeatPos < y.BeatPos)
                            return -1;
                        else if (x.BeatPos > y.BeatPos)
                            return 1;
                        else
                            return 0;
                    });
                    MubTempoMarker temp;
                    for (int i=1; i<tempoMarkers.Count; ++i)
                    {
                        temp = tempoMarkers[i];
                        temp.AbsolutePos = tempoMarkers[i - 1].GetAbsolutePos(tempoMarkers[i].BeatPos);
                        tempoMarkers[i] = temp;
                    }
                }
            }

            if (tempoMarkers == null)
            {
                track.Add(new TempoEvent(chartUsPerQuarterNote, 0));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> CreateTrack()
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("NOTES", MetaEventType.SequenceTrackName, 0));

            foreach (var entry in _mub.Entries)
            {
                long start = NotePosToTicks(entry.Start);
                long end = NotePosToTicks(entry.Start + entry.Length);

                // DJH2 effect type - 0x05FFFFFF through 0x06000009
                // Should not be added to the notes track
                if ((entry.Modifier & 0xFF000000) == 0x06000000
                    || entry.Modifier == 0x05FFFFFF) continue;

                // Add just the Chart BPM text event, ignore the tempo markers
                if ((entry.Modifier & 0xFF000000) == 0x0B000000)
                {
                    if (entry.Modifier == 0x0B000002)
                    {
                        float chartBpm = BitConverter.ToSingle(BitConverter.GetBytes(entry.Data), 0);
                        track.Add(new NAudio.Midi.TextEvent(chartBpm.ToString(), MetaEventType.CuePoint, 0));
                    }
                    continue;
                }

                if ((entry.Modifier & 0xFFFFFF) == 0xFFFFFF)
                {
                    // Text Event?
                    if (!string.IsNullOrEmpty(entry.Text))
                    {
                        // Author?
                        if ((entry.Modifier & 0xFF000000) == 0x0A000000)
                        {
                            track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.Copyright, 0));
                        }
                        // section
                        else if ((entry.Modifier & 0xFF000000) == 0x09000000)
                        {
                            track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));
                        }
                        // unknown
                        else
                        {
                            track.Add(new NAudio.Midi.TextEvent($"UNK_{entry.Modifier:X}_{entry.Text}", MetaEventType.Marker, start));
                        }
                    }
                    // 0xFFFFFFFF?
                    else if ((entry.Modifier & 0xFF000000) == 0xFF000000)
                    {
                        // ignore
                    }
                    else
                    {
                        track.Add(new NAudio.Midi.TextEvent($"UNK_{entry.Modifier:X}", MetaEventType.Marker, start));
                    }
                    continue;
                }

                // Lyric Marker
                if ((entry.Modifier & 0xFFFFFF00) == 0x1100)
                {
                    string lyricMarker;
                    int markerMod = entry.Modifier & 0xFF;
                    if (markerMod == 1)
                        lyricMarker = "LYRIC_PAGE";
                    else if (markerMod == 3 || markerMod == 4)
                        lyricMarker = "LYRIC_COLOR";
                    else
                        lyricMarker = $"LYRIC_UNK_{markerMod}";
                    track.Add(new NAudio.Midi.TextEvent(lyricMarker, MetaEventType.Marker, start));
                }

                if (entry.Length <= 0) continue;

                // Lyric
                if ((entry.Modifier & 0xFFFFFF00) == 0x1000)
                {
                    if (!string.IsNullOrEmpty(entry.Text))
                        track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.Lyric, start));
                }

                int noteMod = entry.Modifier & 0xFF;
                int velocity = entry.Data + 1;
                if (velocity > 127)
                {
                    velocity = 127;
                }
                else if (velocity < 1)
                {
                    velocity = 1;
                }
                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, noteMod, velocity));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, noteMod, velocity));
            }

            track.Sort((x, y) =>
            {
                if (x.AbsoluteTime < y.AbsoluteTime)
                    return -1;
                else if (x.AbsoluteTime > y.AbsoluteTime)
                    return 1;
                else
                    return 0;
            });

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> CreateEffectsTrack()
        {
            List<MidiEvent> effects = null;

            foreach (var entry in _mub.Entries)
            {
                long start = NotePosToTicks(entry.Start);
                long end = NotePosToTicks(entry.Start + entry.Length);

                if ((entry.Modifier & 0xFF000000) == 0x06000000 || entry.Modifier == 0x05FFFFFF)
                {
                    // Filter effect doesn't actually need an effect note, but sometimes 0x05FFFFFF is used for Filter.
                    // For the midi, using pitch 0x7F (127) for Filter is good enough.
                    int effectMod = entry.Modifier & 0x7F;
                    int velocity = entry.Data + 1;
                    if (velocity > 127)
                    {
                        velocity = 127;
                    }
                    else if (velocity < 1)
                    {
                        velocity = 1;
                    }

                    if (effects == null)
                    {
                        effects = new List<MidiEvent>();
                        effects.Add(new NAudio.Midi.TextEvent("EFFECTS", MetaEventType.SequenceTrackName, 0));
                    }
                    effects.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, effectMod, velocity));
                    effects.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, effectMod, velocity));
                    continue;
                }
            }

            if (effects != null)
            {
                effects.Sort((x, y) =>
                {
                    if (x.AbsoluteTime < y.AbsoluteTime)
                        return -1;
                    else if (x.AbsoluteTime > y.AbsoluteTime)
                        return 1;
                    else
                        return 0;
                });

                // Adds end track
                effects.Add(new MetaEvent(MetaEventType.EndTrack, 0, effects.Last().AbsoluteTime));
            }
            return effects;
        }
    }
}
