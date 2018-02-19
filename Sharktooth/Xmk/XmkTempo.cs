using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Xmk
{
    public struct XmkTempo
    {
        public uint Ticks; // Absolute ticks (Uses 960ppq) -> Divide by 2 when using 480ppq
        public float Start; // In seconds
        public uint MicroPerQuarter; // Micro seconds per quarter note
        public double BPM => 60000000.0d / MicroPerQuarter;

        public XmkTempo(uint ticks, float start, uint mpq)
        {
            Ticks = ticks;
            Start = start;
            MicroPerQuarter = mpq;
        }

        public override string ToString() => $"{Start:0.000}s, {MicroPerQuarter}mpq, {Ticks}ticks, {BPM:0.000}bpm";
    }
}
