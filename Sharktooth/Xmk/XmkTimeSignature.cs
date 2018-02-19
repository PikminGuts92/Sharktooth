using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Xmk
{
    public struct XmkTimeSignature
    {
        public uint Ticks; // Absolute ticks (Uses 960ppq) -> Divide by 2 when using 480ppq
        public int Measure; // Index starting at 1
        public int Numerator;
        public int Denominator;

        public XmkTimeSignature(uint ticks, int measure, int num, int den)
        {
            Ticks = ticks;
            Measure = measure;
            Numerator = num;
            Denominator = den;
        }

        public override string ToString() => $"{Ticks}, {Numerator}/{Denominator}, {Measure}";
    }
}
