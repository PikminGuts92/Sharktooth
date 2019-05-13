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

        public MubImport(string midPath)
        {
            _mid = new MidiFile(midPath);
        }
        
        public Mub ExportToMub()
        {
            var existingTracks = _mid.Events
                .Skip(1)
                .ToDictionary(x => GetTrackName(x), y => y);

            var noteTrack = existingTracks.Keys
                .Where(x => x == "NOTES")
                .Select(x => existingTracks[x])
                .FirstOrDefault();

            if (noteTrack == null)
                throw new Exception("Can't find \"NOTES\" midi track");

            // TODO: Convert from PART GUITAR, PART VOCALS, and custom midi spec for DJ notes

            var mubNotes = new List<MubEntry>();

            var notes = noteTrack
                .Where(x => x is NoteOnEvent)
                .Select(x => x as NoteOnEvent);

            foreach (var note in notes)
            {
                if (note.Velocity <= 0)
                    continue;

                mubNotes.Add(new MubEntry((note.AbsoluteTime / (DeltaTicksPerQuarter * 4)),
                    note.NoteNumber,
                    (note.NoteLength / (DeltaTicksPerQuarter * 4))));
            }

            var metaEvents = noteTrack
                .Where(x => x is TextEvent te
                    && (te.MetaEventType == MetaEventType.TextEvent
                        || te.MetaEventType == MetaEventType.Lyric))
                .Select(x => x as TextEvent);

            foreach (var textNote in metaEvents)
            {
                mubNotes.Add(new MubEntry((textNote.AbsoluteTime / (DeltaTicksPerQuarter * 4)),
                    0x09_FF_FF_FF,
                    0.0f,
                    textNote.Text));
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

        public float DeltaTicksPerQuarter => _mid.DeltaTicksPerQuarterNote;
    }
}
