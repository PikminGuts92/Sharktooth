using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Mub
{
    public struct MubTempoMarker
    {
        public MubTempoMarker(double beatPos, int usPerQuarterNote, int chartUsPerQuarterNote)
        {
            ChartUsPerQuarterNote = chartUsPerQuarterNote;
            UsPerQuarterNote = usPerQuarterNote;
            BeatPos = beatPos;
            AbsolutePos = beatPos;
        }

        public int ChartUsPerQuarterNote { get; }
        public int UsPerQuarterNote { get; }
        public double BeatPos { set;  get; }
        public double AbsolutePos { set; get; }

        public double GetBeatPos(double notePos)
        {
            return BeatPos + (notePos - AbsolutePos) * ChartUsPerQuarterNote / UsPerQuarterNote;
        }

        public double GetAbsolutePos(double notePos)
        {
            return AbsolutePos + (notePos - BeatPos) * UsPerQuarterNote / ChartUsPerQuarterNote;
        }
    }
}
