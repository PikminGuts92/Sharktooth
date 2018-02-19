using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Xmk
{
    public struct XmkTimeSignature
    {
        public uint Ticks;
        public int Unknown;
        public int Numerator;
        public int Denominator;

        public XmkTimeSignature(uint ticks, int unknown, int num, int den)
        {
            Ticks = ticks;
            Unknown = unknown;
            Numerator = num;
            Denominator = den;
        }

        public override string ToString() => $"{Ticks}, {Numerator}/{Denominator}, {Unknown}";
    }
}
