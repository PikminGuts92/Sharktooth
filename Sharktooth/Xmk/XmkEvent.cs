using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Xmk
{
    // 24 bytes
    public struct XmkEvent
    {
        public uint Unknown1;
        public ushort Unknown2;
        public byte Unknown3;
        public byte Pitch;
        public float Start; // In seconds
        public float End;
        public uint Unknown4;
        public string Text;

        public override string ToString() => $"{Start:0.000}s, {End:0.000}s, {Pitch} , \"{Text}\", [{Unknown1} {Unknown2} {Unknown3} {Unknown4}]";
    }
}
