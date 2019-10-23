using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Mub
{
    public struct MubEntry
    {
        public MubEntry(float start, int mod, float length, int data = 0, string text = "")
        {
            Start = start;
            Modifier = mod;
            Length = length;
            Data = data;
            Text = text;
        }

        public float Start { get; set; } // Measure percentage, 0-index
        public int Modifier { get; set; }
        public float Length { get; set; }
        public int Data { get; set; }
        public string Text { get; set; }

        public override string ToString() => $"{Start:0.000}, 0x{Modifier:X8}, {Length:0.000}, 0x{Data:X8}, \"{Text}\"";
    }
}
