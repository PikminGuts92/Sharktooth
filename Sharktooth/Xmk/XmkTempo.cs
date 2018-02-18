using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Xmk
{
    public struct XmkTempo
    {
        public float Start; // In seconds
        public uint MicroPerQuarter; // Micro seconds per quarter note
        public uint Ticks; // ??
        public double BPM => 60000000.0d / MicroPerQuarter;

        public XmkTempo(float start, uint mpq, uint ticks)
        {
            Start = start;
            MicroPerQuarter = mpq;
            Ticks = ticks;
        }

        public override string ToString() => $"{Start:0.000}s, {MicroPerQuarter}mpq, {Ticks}ticks, {BPM:0.000}bpm";
    }
}
