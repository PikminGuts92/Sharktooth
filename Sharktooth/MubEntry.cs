using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth
{
    public struct MubEntry
    {
        public MubEntry(float start, int mod, float length, string text = "")
        {
            Start = start;
            Modifier = mod;
            Length = length;
            Text = text;
        }

        public float Start { get; set; } // Measure percentage, 0-index
        public int Modifier { get; set; }
        public float Length { get; set; }
        public string Text { get; set; }

        public override string ToString() => $"{Start:0.000}, 0x{Modifier:X8}, {Length:0.000}, \"{Text}\"";
    }
}
