using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharktooth;

namespace Mub2Mid
{
    class Program
    {
        static void Main(string[] args)
        {
            Mub mub = Mub.FromFile(args[0]);
            MIDIExport mid = new MIDIExport(mub);
            mid.Export(args[1]);
        }
    }
}
