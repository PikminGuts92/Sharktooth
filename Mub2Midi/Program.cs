using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharktooth;

namespace Mub2Midi
{
    class Program
    {
        static void Main(string[] args)
        {
            Mub mub = Mub.FromFile(args[0]);
        }
    }
}
