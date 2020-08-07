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

        private readonly int CALCULATED_QUANTIZATION;

        private XmkExportOptions _exportOptions;

        private readonly List<TempoIndex> _tempoIdx = new List<TempoIndex>();
        private List<Xmk> _xmks;

        // Midi pitch mappings
        private MidiMapping _guitarMap = MidiMapping.CreateGuitar3();
        private MidiMapping _guitarTouchMap = MidiMapping.CreateGuitar5();
        private MidiMapping _drumsMap = MidiMapping.CreateRBDrums();

        public XmkExport(IList<Xmk> xmks) : this(xmks, XmkExportOptions.Default) { }

        public XmkExport(IList<Xmk> xmks, XmkExportOptions options)
        {
            _xmks = new List<Xmk>(xmks);
            _exportOptions = options;

            CALCULATED_QUANTIZATION = (int)(DELTA_TICKS_PER_MEASURE * options.Quantization);
        }

        public void Export(string path)
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

            if (!_exportOptions.Remap)
            {
                for (int i = 0; i < _xmks.Count; i++)
                    mid.AddTrack(CreateTrack(_xmks[i], i));
            }
            else
            {
                var eventsIdx = -1;
                for (int i = 0; i < _xmks.Count; i++)
                {
                    Xmk xmk = _xmks[i];
                    string trackName = !string.IsNullOrEmpty(xmk.Name) ? xmk.Name : $"NOTES {i}";

                    // Sets remapping and track name
                    switch (trackName.ToLower())
                    {
                        case "control":
                            // EVENTS
                            mid.AddTrack(ParseEvents(xmk));
                            eventsIdx = i + 1;
                            continue;
                        case "guitar_3x2":
                            // PART GUITAR GHL
                            mid.AddTrack(ParseGuitar3(xmk));
                            continue;
                        case "touchdrums":
                            // PART DRUMS
                            mid.AddTrack(ParseDrums(xmk));
                            continue;
                        case "touchguitar":
                            // PART GUITAR
                            mid.AddTrack(ParseGuitar5(xmk));
                            continue;
                        case "vocals":
                            // PART VOCALS
                            mid.AddTrack(ParseVocals(xmk));
                            continue;
                    }

                    mid.AddTrack(CreateTrack(xmk, i));
                }

                // Updates EVENTS track
                var firstPlay = mid.SelectMany(x => x)
                    .Where(y => y is TextEvent && ((TextEvent)y).Text == "[play]")
                    .OrderBy(z => z.AbsoluteTime)
                    .FirstOrDefault();

                var lastIdle = mid.SelectMany(x => x)
                    .Where(y => y is TextEvent && ((TextEvent)y).Text == "[idle]")
                    .OrderByDescending(z => z.AbsoluteTime)
                    .FirstOrDefault();

                if (eventsIdx != -1 && firstPlay != null)
                {
                    var events = mid[eventsIdx];
                    var nextEvent = events.OrderBy(x => x.AbsoluteTime).First(y => y.AbsoluteTime > firstPlay.AbsoluteTime);
                    var nextEventIdx = events.IndexOf(nextEvent);

                    events.Insert(nextEventIdx, new TextEvent("[music_start]", MetaEventType.TextEvent, firstPlay.AbsoluteTime));
                }

                if (eventsIdx != -1 && lastIdle != null)
                {
                    var events = mid[eventsIdx];
                    var trackEnd = events.Last();
                    trackEnd.AbsoluteTime = lastIdle.AbsoluteTime; // Updates end track position
                    events.Remove(trackEnd);
                    
                    events.Add(new TextEvent("[music_end]", MetaEventType.TextEvent, lastIdle.AbsoluteTime));
                    events.Add(new TextEvent("[end]", MetaEventType.TextEvent, lastIdle.AbsoluteTime));
                    events.Add(trackEnd); // Adds end track back
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
                var idxEntry = new TempoIndex(0, 0, 120, (60000000 / 120));
                //track.Add(new NAudio.Midi.TempoEvent(idxEntry.MicroPerQuarter, idxEntry.AbsoluteTime));
                _tempoIdx.Add(idxEntry);
            }

            // Adds tempo changes
            if (tempos.Count > 0)
            {
                var firstTempo = tempos.First();
                var idxEntry = new TempoIndex()
                {
                    AbsoluteTime = _tempoIdx.Count > 0 ? (firstTempo.Ticks / 2) : 0,
                    RealTime = firstTempo.Start * 1000,
                    BPM = firstTempo.BPM,
                    MicroPerQuarter = (int)firstTempo.MicroPerQuarter
                };

                track.Add(new NAudio.Midi.TempoEvent(idxEntry.MicroPerQuarter, idxEntry.AbsoluteTime));
                _tempoIdx.Add(idxEntry);

                foreach (var tempoEntry in tempos.Skip(1))
                {
                    idxEntry = new TempoIndex()
                    {
                        AbsoluteTime = (tempoEntry.Ticks / 2),
                        RealTime = tempoEntry.Start * 1000,
                        BPM = tempoEntry.BPM,
                        MicroPerQuarter = (int)tempoEntry.MicroPerQuarter
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
            if (CALCULATED_QUANTIZATION > 0 && absoluteTicks % CALCULATED_QUANTIZATION != 0)
            {
                long before = absoluteTicks % CALCULATED_QUANTIZATION;
                long after = CALCULATED_QUANTIZATION - before;

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
                if (!string.IsNullOrEmpty(entry.Text)) continue; // Don't write text events
                    //track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));

                if ((end - start) <= 0 || entry.Pitch > 127) continue;

                if ((entry.Unknown2 & 2) == 2) // Observed 0x02, 0xCA
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

            // Adds play event
            var firstNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderBy(y => y.AbsoluteTime).FirstOrDefault();
            if (firstNote != null)
            {
                var idx = track.IndexOf(firstNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[play]", MetaEventType.TextEvent, firstNote.AbsoluteTime));
            }

            // Adds idle event (end)
            var lastNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderByDescending(y => y.AbsoluteTime).FirstOrDefault();
            if (lastNote != null)
            {
                var idx = track.IndexOf(lastNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[idle]", MetaEventType.TextEvent, lastNote.AbsoluteTime));
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

        private List<MidiEvent> ParseGuitar5(Xmk xmk, bool guitar = true)
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
                if (!string.IsNullOrEmpty(entry.Text)) continue; // Don't write text events
                    //track.Add(new NAudio.Midi.TextEvent(entry.Text, MetaEventType.TextEvent, start));

                if ((end - start) <= 0 || entry.Pitch > 127) continue;
                
                int pitchRemap = map[entry.Pitch];
                if (pitchRemap == -1) continue;

                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, pitchRemap, velocity));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, pitchRemap, velocity));
            }

            // Adds play event
            var firstNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderBy(y => y.AbsoluteTime).FirstOrDefault();
            if (firstNote != null)
            {
                var idx = track.IndexOf(firstNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[play]", MetaEventType.TextEvent, firstNote.AbsoluteTime));
            }

            // Adds idle event (end)
            var lastNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderByDescending(y => y.AbsoluteTime).FirstOrDefault();
            if (lastNote != null)
            {
                var idx = track.IndexOf(lastNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[idle]", MetaEventType.TextEvent, lastNote.AbsoluteTime));
            }

            // Adds end track
            track.Add(new MetaEvent(MetaEventType.EndTrack, 0, track.Last().AbsoluteTime));
            return track;
        }

        private List<MidiEvent> ParseDrums(Xmk xmk)
        {
            MidiMapping map = _drumsMap;
            List<MidiEvent> track = new List<MidiEvent>();
            track.Add(new NAudio.Midi.TextEvent("PART DRUMS", MetaEventType.SequenceTrackName, 0));

            // Standard mix events (From RBN template)
            track.Add(new NAudio.Midi.TextEvent("[mix 0 drums0]", MetaEventType.TextEvent, 0));
            track.Add(new NAudio.Midi.TextEvent("[mix 1 drums0]", MetaEventType.TextEvent, 0));
            track.Add(new NAudio.Midi.TextEvent("[mix 2 drums0]", MetaEventType.TextEvent, 0));
            track.Add(new NAudio.Midi.TextEvent("[mix 3 drums0]", MetaEventType.TextEvent, 0));

            foreach (var entry in xmk.Entries)
            {
                long start = GetAbsoluteTime(entry.Start * 1000);
                long end = GetAbsoluteTime(entry.End * 1000);
                int velocity = 100;

                // Text event?
                if (!string.IsNullOrEmpty(entry.Text)) continue; // Don't write text events
                if ((end - start) <= 0 || entry.Pitch > 127) continue;

                int pitchRemap = map[entry.Pitch];
                if (pitchRemap == -1) continue;

                track.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, pitchRemap, velocity));
                track.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, pitchRemap, velocity));
            }

            // Adds play event
            var firstNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderBy(y => y.AbsoluteTime).FirstOrDefault();
            if (firstNote != null)
            {
                var idx = track.IndexOf(firstNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[play]", MetaEventType.TextEvent, firstNote.AbsoluteTime));
            }

            // Adds idle event (end)
            var lastNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderByDescending(y => y.AbsoluteTime).FirstOrDefault();
            if (lastNote != null)
            {
                var idx = track.IndexOf(lastNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[idle]", MetaEventType.TextEvent, lastNote.AbsoluteTime));
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

            var phraseEvents = new List<NoteEvent>();

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

                    phraseEvents.Add(new NoteEvent(start, 1, MidiCommandCode.NoteOn, VOCALS_PHRASE, velocity));
                    phraseEvents.Add(new NoteEvent(end, 1, MidiCommandCode.NoteOff, VOCALS_PHRASE, velocity));
                }
            }

            // Extends phrases
            for (int i = 1; i < phraseEvents.Count - 1; i += 2)
            {
                // i = end, i+1 = start
                var endTime = phraseEvents[i + 1].AbsoluteTime;
                phraseEvents[i].AbsoluteTime = endTime;
            }

            if (phraseEvents.Count > 0)
            {
                // Extends first phrase
                phraseEvents.First().AbsoluteTime = 0;

                // Extends last phrase
                phraseEvents.Last().AbsoluteTime = Math.Max(phraseEvents.Last().AbsoluteTime, track.Max(x => x.AbsoluteTime)) + DELTA_TICKS_PER_QUARTER / 8;
            }
            else
            {
                // No phrases found (Create one)
                phraseEvents.Add(new NoteEvent(0, 1, MidiCommandCode.NoteOn, VOCALS_PHRASE, 100));
                phraseEvents.Add(new NoteEvent(track.Max(x => x.AbsoluteTime) + DELTA_TICKS_PER_QUARTER / 8, 1, MidiCommandCode.NoteOff, VOCALS_PHRASE, 100));
            }

            // Shrinks phrases
            for (int i = 0; i < phraseEvents.Count; i += 2)
            {
                NoteEvent start = phraseEvents[i];
                NoteEvent end = phraseEvents[i + 1];

                var betweenNotes = track.Where(x => x is NoteEvent
                    && ((NoteEvent)x).NoteNumber != VOCALS_PHRASE
                    && x.AbsoluteTime >= start.AbsoluteTime
                    && x.AbsoluteTime <= end.AbsoluteTime).ToList();

                if (betweenNotes.Count <= 0)
                    continue; // No notes between phrases

                var startEvent = betweenNotes
                    .Where(x => ((NoteEvent)x).CommandCode == MidiCommandCode.NoteOn)
                    .OrderBy(y => y.AbsoluteTime).FirstOrDefault();

                var endEvent = betweenNotes
                    .Where(x => ((NoteEvent)x).CommandCode == MidiCommandCode.NoteOff)
                    .OrderBy(y => y.AbsoluteTime).LastOrDefault();

                if (startEvent == null || endEvent == null || startEvent.AbsoluteTime > endEvent.AbsoluteTime)
                    continue; // No full notes between phrases

                // 1/128th note
                start.AbsoluteTime = startEvent.AbsoluteTime - (DELTA_TICKS_PER_QUARTER / 32);
                end.AbsoluteTime = endEvent.AbsoluteTime + (DELTA_TICKS_PER_QUARTER / 32);

                track.Add(start);
                track.Add(end);
            }

            // Adds play event
            var firstNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderBy(y => y.AbsoluteTime).FirstOrDefault();
            if (firstNote != null)
            {
                var idx = track.IndexOf(firstNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[play]", MetaEventType.TextEvent, firstNote.AbsoluteTime));
            }

            // Adds idle event (end)
            var lastNote = track.Where(x => x is NoteEvent).Select(y => y as NoteEvent).OrderByDescending(y => y.AbsoluteTime).FirstOrDefault();
            if (lastNote != null)
            {
                var idx = track.IndexOf(lastNote);
                track.Insert(idx, new NAudio.Midi.TextEvent("[idle]", MetaEventType.TextEvent, lastNote.AbsoluteTime));
            }

            // Sort by absolute time (And ensure track name is first event)
            track.Sort((x, y) => (int)(x is NAudio.Midi.TextEvent
                                       && ((NAudio.Midi.TextEvent)x).MetaEventType == MetaEventType.SequenceTrackName
                                       ? int.MinValue : x.AbsoluteTime - y.AbsoluteTime));

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
