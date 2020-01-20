using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

namespace Sharktooth.Mub
{
    public class MubImport
    {
        private MidiFile _mid;
        private List<MubTempoMarker> tempoMarkers;

        public MubImport(string midPath)
        {
            _mid = new MidiFile(midPath);
        }

        private double NoteTicksToPos(long ticks, bool useTempoMarkers = true)
        {
            double ticksPerMeasure = DeltaTicksPerQuarter * 4; // Assume 4/4
            double pos = ticks / ticksPerMeasure;

            if (useTempoMarkers && tempoMarkers != null)
            {
                for (int i = 0; i < tempoMarkers.Count; ++i)
                {
                    if (i == tempoMarkers.Count - 1 || tempoMarkers[i + 1].BeatPos > pos)
                    {
                        return tempoMarkers[i].GetAbsolutePos(pos);
                    }
                }
                throw new Exception("Error converting note position using tempo markers");
            }
            return pos;
        }

        public Mub ExportToMub()
        {
            Dictionary<string, IList<MidiEvent>> existingTracks;
            try
            {
                existingTracks = _mid.Events
                    .Skip(1)
                    .ToDictionary(x => GetTrackName(x), y => y);
            }
            catch (NullReferenceException)
            {
                throw new Exception("MIDI track is missing a \"NOTES\" or \"EFFECTS\" track name text event");
            }

            var tempoTrack = _mid.Events[0];
            int chartUsPerQuarterNote = 0;
            float bpm = 0;

            var noteTrack = existingTracks.Keys
                .Where(x => x == "NOTES")
                .Select(x => existingTracks[x])
                .FirstOrDefault();

            if (noteTrack == null)
                throw new Exception("Can't find \"NOTES\" midi track");

            // TODO: Convert from PART GUITAR, PART VOCALS, and custom midi spec for DJ notes

            var mubNotes = new List<MubEntry>();

            var bpmEvents = noteTrack
                .Where(x => x is TextEvent te
                    && (te.MetaEventType == MetaEventType.CuePoint))
                .Select(x => x as TextEvent);

            // Look for BPM Cue text event
            foreach (var textNote in bpmEvents)
            {
                if (bpm != 0)
                {
                    throw new Exception("Too many Cue (BPM) events");
                }
                try
                {
                    bpm = float.Parse(textNote.Text, System.Globalization.CultureInfo.InvariantCulture);
                    if (bpm <= 0)
                    {
                        throw new Exception("BPM cannot be 0 or negative");
                    }
                    chartUsPerQuarterNote = (int)Math.Round(60000000 / bpm);
                }
                catch (FormatException)
                {
                    throw new Exception($"Invalid BPM found in Cue TextEvent: {textNote.Text}");
                }
            }

            var tempoEvents = tempoTrack
            .Where(x => x is TempoEvent ts)
            .Select(x => x as TempoEvent);

            // Make tempomap
            foreach (var tempoEvent in tempoEvents)
            {
                double pos = NoteTicksToPos(tempoEvent.AbsoluteTime, false);
                int usPerQuarterNote = tempoEvent.MicrosecondsPerQuarterNote;
                if (tempoMarkers == null)
                {
                    tempoMarkers = new List<MubTempoMarker>();
                    if (bpm == 0)
                    {
                        chartUsPerQuarterNote = usPerQuarterNote;
                        bpm = (float)60000000 / chartUsPerQuarterNote;
                        mubNotes.Add(new MubEntry(0.0f,
                        0x0B_00_00_02,
                        0.0f,
                        BitConverter.ToInt32(BitConverter.GetBytes(bpm), 0)));
                    }
                }
                tempoMarkers.Add(new MubTempoMarker(pos, usPerQuarterNote, chartUsPerQuarterNote));
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
                for (int i = 0; i < tempoMarkers.Count; ++i)
                {
                    if (i > 0)
                    {
                        temp = tempoMarkers[i];
                        temp.AbsolutePos = tempoMarkers[i - 1].GetAbsolutePos(tempoMarkers[i].BeatPos);
                        tempoMarkers[i] = temp;
                    }

                    mubNotes.Add(new MubEntry((float)tempoMarkers[i].BeatPos,
                    0x0B_00_00_01,
                    0.0f,
                    tempoMarkers[i].UsPerQuarterNote));
                }
            }

            var metaEvents = noteTrack
            .Where(x => x is TextEvent te
                && (te.MetaEventType == MetaEventType.TextEvent
                    || te.MetaEventType == MetaEventType.Copyright
                    || te.MetaEventType == MetaEventType.CuePoint
                    || te.MetaEventType == MetaEventType.Marker))
            .Select(x => x as TextEvent);

            foreach (var textNote in metaEvents)
            {
                // Sections
                if (textNote.MetaEventType == MetaEventType.TextEvent)
                {
                    mubNotes.Add(new MubEntry((float)NoteTicksToPos(textNote.AbsoluteTime),
                        0x09_FF_FF_FF,
                        0.0f,
                        0,
                        textNote.Text));
                }
                // Author
                else if (textNote.MetaEventType == MetaEventType.Copyright)
                {
                    mubNotes.Add(new MubEntry((float)NoteTicksToPos(textNote.AbsoluteTime),
                    0x0A_FF_FF_FF,
                    0.0f,
                    0,
                    textNote.Text));
                }
                // BPM
                // don't include if there are no tempomarkers for some reason, since we can't guarantee
                // the cue BPM & chart BPM are the same.
                else if (textNote.MetaEventType == MetaEventType.CuePoint && tempoMarkers != null)
                {
                    mubNotes.Add(new MubEntry((float)NoteTicksToPos(textNote.AbsoluteTime),
                    0x0B_00_00_02,
                    0.0f,
                    BitConverter.ToInt32(BitConverter.GetBytes(bpm),0)));
                }
                // Lyric Page
                else if (textNote.MetaEventType == MetaEventType.Marker && textNote.Text.ToUpper() == "LYRIC_PAGE")
                {
                    mubNotes.Add(new MubEntry((float)NoteTicksToPos(textNote.AbsoluteTime),
                    0x00_00_11_01,
                    0.0f,
                    0));
                }
            }

            // 0xFFFFFFFF note at beginning of chart needed
            mubNotes.Add(new MubEntry(0.0f,
                                -1, // 0xFFFFFFFF
                                0.0f,
                                0));

            var lyricEvents = new Queue<TextEvent>(noteTrack
            .Where(x => x is TextEvent te
                && (te.MetaEventType == MetaEventType.Lyric))
            .Select(x => x as TextEvent));

            var lyricColorEvents = new Queue<TextEvent>(noteTrack
            .Where(x => x is TextEvent te
                && (te.MetaEventType == MetaEventType.Marker && te.Text.ToUpper() == "LYRIC_COLOR"))
            .Select(x => x as TextEvent));

            var notes = noteTrack
                .Where(x => x is NoteOnEvent)
                .Select(x => x as NoteOnEvent);

            foreach (var note in notes)
            {
                double start = NoteTicksToPos(note.AbsoluteTime);
                double end = NoteTicksToPos(note.AbsoluteTime + note.NoteLength);
                int noteNumber = note.NoteNumber;
                string lyricString = "";

                if (noteNumber == 3 || noteNumber == 4)
                {
                    if (lyricColorEvents.Count > 0)
                    {
                        var lyricColor = lyricColorEvents.Peek();
                        if (lyricColor.AbsoluteTime == note.AbsoluteTime)
                        {
                            lyricColorEvents.Dequeue();
                            noteNumber += 0x1100;
                        }
                        else if (lyricColor.AbsoluteTime < note.AbsoluteTime)
                        {
                            throw new Exception($"LYRIC_COLOR marker not associated with a MIDI note: {NoteTicksToPos(lyricColor.AbsoluteTime, false) }");
                        }
                    }
                }
                else if (lyricEvents.Count > 0)
                {
                    var lyricEvent = lyricEvents.Peek();
                    if (lyricEvent.AbsoluteTime == note.AbsoluteTime)
                    {
                        lyricString = lyricEvent.Text;
                        lyricEvents.Dequeue();
                        noteNumber += 0x1000;
                    }
                    else if (lyricEvent.AbsoluteTime < note.AbsoluteTime)
                    {
                        throw new Exception($"Lyric \"{lyricEvent.Text}\"not associated with a MIDI note: {NoteTicksToPos(lyricEvent.AbsoluteTime, false) }");
                    }
                }

                mubNotes.Add(new MubEntry((float)start,
                    noteNumber,
                    (float)(end - start),
                    note.Velocity - 1,
                    lyricString));
            }

            // DJ Hero 2 effects
            var effectTrack = existingTracks.Keys
                .Where(x => x == "EFFECTS")
                .Select(x => existingTracks[x])
                .FirstOrDefault();

            if (effectTrack != null)
            {
                var effects = effectTrack
                    .Where(x => x is NoteOnEvent)
                    .Select(x => x as NoteOnEvent);

                foreach (var effect in effects)
                {
                    double start = NoteTicksToPos(effect.AbsoluteTime);
                    double end = NoteTicksToPos(effect.AbsoluteTime + effect.NoteLength);
                    mubNotes.Add(new MubEntry((float)start,
                        effect.NoteNumber + 0x06_00_00_00,
                        (float)(end - start),
                        effect.Velocity - 1));
                }
            }

            return new Mub()
            {
                Version = 2,
                Entries = mubNotes
                    .OrderBy(x => x.Start)
                    .ThenByDescending(x => x.Modifier >> 24)
                    .ThenBy(x => x.Modifier & 0xFF)
                    .ToList()
            };
        }

        private static string GetTrackName(IList<MidiEvent> track)
        {
            var trackNameEv = track.FirstOrDefault(y => y.CommandCode == MidiCommandCode.MetaEvent
                        && (y as TextEvent).MetaEventType == MetaEventType.SequenceTrackName);

            return (trackNameEv as TextEvent)?.Text;
        }

        public double DeltaTicksPerQuarter => _mid.DeltaTicksPerQuarterNote;
    }
}
