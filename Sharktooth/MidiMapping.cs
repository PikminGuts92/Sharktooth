using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth
{
    public class MidiMapping
    {
        private readonly Dictionary<int, int> _mappings;

        private MidiMapping()
        {
            _mappings = new Dictionary<int, int>();
        }

        public static MidiMapping CreateGuitar3()
        {
            MidiMapping mid = new MidiMapping();

            // Expert guitar
            mid._mappings.Add(74, 116); // SP
            mid._mappings.Add(69,  94); // Open
            mid._mappings.Add(64,  97); // W3
            mid._mappings.Add(63, 100); // B3
            mid._mappings.Add(62,  96); // W2
            mid._mappings.Add(61,  99); // B2
            mid._mappings.Add(60,  95); // W1
            mid._mappings.Add(59,  98); // B1

            return mid;
        }
    }
}
