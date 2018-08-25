using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;

namespace Sharktooth.Xmk
{
    public class XmkExport
    {
        private const int DELTA_TICKS_PER_QUARTER = 480;
        private const int DELTA_TICKS_PER_MEASURE = DELTA_TICKS_PER_QUARTER * 4;
        private const int QUANTIZATION = 128; // Should be power of 2

        private readonly List<TempoIndex> _tempoIdx = new List<TempoIndex>();
        private List<Xmk> _xmks;

        // Midi pitch mappings
        private MidiMapping _guitarMap = MidiMapping.CreateGuitar3();
        private MidiMapping _guitarTouchMap = MidiMapping.CreateGuitar6();

        public XmkExport(Xmk xmk)
        {
            _xmks = new List<Xmk>();
            _xmks.Add(xmk);
        }

        public XmkExport(List<Xmk> xmks)
        {
            _xmks = new List<Xmk>(xmks);
        }

        public void Export(string path, bool remap = false)
        {
            MidiEventCollection mid = new MidiEventCollection(1, DELTA_TICKS_PER_QUARTER);
            _xmks.Sort((x, y) => GetSortNumber(x.Name) - GetSortNumber(y.Name));

            int GetSortNumber(string name)
            {
                switch (name)
                {
                    case "touchdrums":
                        return 1;
                    case "touchguitar":
                        return 2;
                    case "guitar_3x2":
                        return 3;
                    case "vocals":
                        return 4;
                    case "control":
                        return 5;
                    default:
                        return 100;
                }
            }

            Xmk firstXmk = _xmks.FirstOrDefault();
            mid.AddTrack(CreateTempoTrack(firstXmk.TempoEntries, firstXmk.TimeSignatureEntries));

            if (!remap)
            {
                for (int i = 0; i < _xmks.Count; i++)
                    mid.AddTrack(CreateTrack(_xmks[i], i));
            }
            else
            {
                for (int i = 0; i < _xmks.Count; i++)
                {
                    Xmk xmk = _xmks[i];
                    string trackName = !string.IsNullOrEmpty(xmk.Name) ? xmk.Name : $"NOTES {i}";

                    // Sets remapping and track name
                    switch (trackName.ToLower())
                    {
                        case "control":
                            trackName = "CONTROL";
                            mid.AddTrack(ParseEvents(xmk));
                            continue;
                        case "guitar_3x2":
                            trackName = "PART GUITAR GHL";
                            mid.AddTrack(ParseGuitar3(xmk));
                            continue;
                        case "touchdrums":
                            trackName = "TOUCH DRUMS";
                            break;
                        case "touchguitar":
                            trackName = "TOUCH GUITAR";
                            mid.AddTrack(ParseGuitar6(xmk));
                            continue;
                        case "vocals":
                            trackName = "PART VOCALS";
                            mid.AddTrack(ParseVocals(xmk));
                            continue;
                    }

                    mid.AddTrack(CreateTrack(xmk, i));
                }
                
                // Generates up/down events for BEAT track (Needed for beat markers in CH and OD in RB)
                mid.AddTrack(GenerateBeatTrack(firstXmk.TimeSignatureEntries, mid));
            }
            
            MidiFile.Export(path, mid);
        }

        private List<MidiEvent> GenerateBeatTrack(List<XmkTimeSignature> timeSigs, MidiEventCollection mid)
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("BEAT", MetaEventType.SequenceTrackName, 0));

            long lastEventOffset = mid.SelectMany(x => x).Select(y => y.AbsoluteTime).Max();
            long offset = 0;

            var tsEndOffsets = timeSigs.Skip(1).Select(x => (long)(x.Ticks / 2)).ToList();
            tsEndOffsets.Add(lastEventOffset);
            
            int tsIdx = 0, currentBeat = 1;

            while (offset < lastEventOffset)
            {
                if (offset >= tsEndOffsets[tsIdx])
                {
                    // New time signature
                    currentBeat = 1;
                    tsIdx++;

                    offset = timeSigs[tsIdx].Ticks / 2;
                }

                var ts = timeSigs[tsIdx];
                int beatSize = DELTA_TICKS_PER_MEASURE / ts.Denominator;
                int eventSize = beatSize / 4;

                if (currentBeat > ts.Numerator) currentBeat = 1;
                int pitch = currentBeat == 1 ? 12 : 13; // 12 = down, 13 = up

                // Adds beat event
                track.Add(new NoteEvent(offset, 1, MidiCommandCode.NoteOn, pitch, 1));
                track.Add(new NoteEvent(offset + eventSize, 1, MidiCommandCode.NoteOff, pitch, 1));

                currentBeat++;
                offset += beatSize;
            }
            
            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> CreateTempoTrack(List<XmkTempo> tempos, List<XmkTimeSignature> timeSignatures)
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
                int q = DELTA_TICKS_PER_MEASURE / QUANTIZATION;
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
            
            // Adds time signature changes
            foreach (var ts in timeSignatures)
            {
                int den = (int)Math.Log(ts.Denominator, 2);

                track.Add(new TimeSignatureEvent(ts.Ticks / 2, ts.Numerator, den, 24, 8));
            }

            // Sort by absolute time (And ensure track name is first event)
            track.Sort((x, y) => (int)(x is NAudio.Midi.TextEvent
                                       && ((NAudio.Midi.TextEvent)x).MetaEventType == MetaEventType.SequenceTrackName
                                       ? int.MinValue : x.AbsoluteTime - y.AbsoluteTime));

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
            int q = DELTA_TICKS_PER_MEASURE / QUANTIZATION;
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

        private List<MidiEvent> CreateTrack(Xmk xmk, int index)
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent(!string.IsNullOrEmpty(xmk.Name) ? xmk.Name : $"NOTES {index}", MetaEventType.SequenceTrackName, 0));

            foreach (var entry in xmk.Entries)
            {
                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                int velocity = entry.Unknown2 == 0 ? 100 : entry.Unknown2 % 128;
                
                // Text event?
                if (!string.IsNullOrEmpty(entry.Text))
                    track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));

                if ((end - start) <= 0 || entry.Pitch > 127) continue;
                
                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, entry.Pitch, velocity));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, entry.Pitch, velocity));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> ParseGuitar3(Xmk xmk, bool guitar = true)
        {
            MidiMapping map = _guitarMap;
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent(guitar ? "PART GUITAR GHL" : "PART BASS GHL", MetaEventType.SequenceTrackName, 0));

            // Tracks HOPO on/off events
            List<NoteOnEvent> hopoNotes = new List<NoteOnEvent>();

            foreach (var entry in xmk.Entries)
            {
                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                int velocity = 100;

                // Text event?
                if (!string.IsNullOrEmpty(entry.Text))
                    track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));

                if ((end - start) <= 0 || entry.Pitch > 127) continue;

                if (entry.Unknown2 == 2)
                {
                    // Barre chord
                    int shift = (entry.Pitch % 2 == 1) ? 1 : -1;

                    track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, map[entry.Pitch + shift], velocity));
                    track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, map[entry.Pitch + shift], velocity));
                }

                int pitchRemap = map[entry.Pitch];
                if (pitchRemap == -1) continue;
                
                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, pitchRemap, velocity));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, pitchRemap, velocity));
                
                int hopoPitch;

                // Sets forced HOPO off pitch
                if (pitchRemap >= 94 && pitchRemap <= 100) hopoPitch = 102; // Expert
                else if (pitchRemap >= 82 && pitchRemap <= 88) hopoPitch = 90; // Hard
                else if (pitchRemap >= 70 && pitchRemap <= 76) hopoPitch = 78; // Medium
                else if (pitchRemap >= 58 && pitchRemap <= 64) hopoPitch = 66; // Easy
                else continue;

                hopoPitch -= (entry.Unknown3 & 0x80) >> 7; // 1 = Forced HOPO
                hopoNotes.Add(new NoteOnEvent(start, 1, hopoPitch, velocity, (int)(end - start)));
            }

            // Flattens HOPO events
            var groupedNotes = hopoNotes.GroupBy(x => x.NoteNumber);
            foreach (var group in groupedNotes)
            {
                // Selects longest note at each absolute time offset
                var notes = group.GroupBy(x => x.AbsoluteTime).Select(y => y.OrderByDescending(z => z.NoteLength).First()).OrderBy(q => q.AbsoluteTime);

                NoteOnEvent prevNote = notes.First();
                track.Add(prevNote);

                foreach (var note in notes.Skip(1))
                {
                    if (note.AbsoluteTime >= prevNote.OffEvent.AbsoluteTime)
                        track.Add(prevNote.OffEvent);
                    else
                        // Overlap detected, insert note off event
                        track.Add(new NoteEvent(note.AbsoluteTime, note.Channel, MidiCommandCode.NoteOff, note.NoteNumber, note.Velocity));

                    track.Add(note);
                    prevNote = note;
                }

                // Adds last note off event
                track.Add(prevNote.OffEvent);
            }

            // Sorts by absolute time
            track.Sort((x, y) =>
            {
                if (x.AbsoluteTime < y.AbsoluteTime)
                    return -1;
                else if (x.AbsoluteTime > y.AbsoluteTime)
                    return 1;

                // Same abs time, note off goes first
                if (x.CommandCode == MidiCommandCode.NoteOff && y.CommandCode == MidiCommandCode.NoteOn)
                    return -1;
                else if (x.CommandCode == MidiCommandCode.NoteOn && y.CommandCode == MidiCommandCode.NoteOff)
                    return 1;
                else
                    return 0;
            });

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> ParseGuitar6(Xmk xmk, bool guitar = true)
        {
            MidiMapping map = _guitarTouchMap;
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent(guitar ? "PART GUITAR" : "PART BASS", MetaEventType.SequenceTrackName, 0));

            foreach (var entry in xmk.Entries)
            {
                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                int velocity = 100;

                // Text event?
                if (!string.IsNullOrEmpty(entry.Text))
                    track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));

                if ((end - start) <= 0 || entry.Pitch > 127) continue;
                
                int pitchRemap = map[entry.Pitch];
                if (pitchRemap == -1) continue;

                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, pitchRemap, velocity));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, pitchRemap, velocity));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> ParseVocals(Xmk xmk)
        {
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("PART VOCALS", MetaEventType.SequenceTrackName, 0));

            const int VOCALS_PHRASE = 105;
            //const int VOCALS_MAX_PITCH = 84;
            const int VOCALS_MIN_PITCH = 36;

            foreach (var entry in xmk.Entries)
            {
                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                int velocity = 100;
                
                if (!string.IsNullOrEmpty(entry.Text) && entry.Unknown3 == 57)
                {
                    // Lyric + pitch event
                    string text = entry.Text;
                    int pitch = entry.Pitch;

                    text = text.Replace("=", string.Empty);
                    text = text.Replace("@", "+");

                    if (entry.Pitch < VOCALS_MIN_PITCH)
                    {
                        text = text + "#";
                        pitch = 60; // Middle C
                    }

                    track.Add(new NAudio.Midi.TextEvent(text, MetaEventType.Lyric, start));
                    track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, pitch, velocity));
                    track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, pitch, velocity));
                }
                else if (entry.Unknown3 == 1 && entry.Pitch == 129)
                {
                    // Vocal phrase
                    if ((end - start) <= 0)
                        end = start + DELTA_TICKS_PER_QUARTER / 4; // 1/16 note

                    track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, VOCALS_PHRASE, velocity));
                    track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, VOCALS_PHRASE, velocity));
                }
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> ParseEvents(Xmk xmk)
        {
            MidiMapping map = _guitarMap;
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("EVENTS", MetaEventType.SequenceTrackName, 0));

            foreach (var entry in xmk.Entries)
            {
                if (string.IsNullOrEmpty(entry.Text) || entry.Unknown3 != 3) continue; // Practice section

                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                string text = GetPracticeName(entry.Text);
                
                track.Add(new NAudio.Midi.TextEvent(text, MetaEventType.TextEvent, start));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private static string GetPracticeName(string practiceValue)
        {
            // Returns known practice section if matched
            var key = practiceValue.Trim().ToLower();
            if (Global.PracticeSections.ContainsKey(key))
                return Global.PracticeSections[key];

            return $"[section {practiceValue}]";
        }
    }
}
