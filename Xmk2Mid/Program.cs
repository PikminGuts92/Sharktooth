using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharktooth.Xmk;

namespace Xmk2Mid
{
    class Program
    {
        static void Main(string[] args)
        {
            Xmk xmk = Xmk.FromFile(args[0]);
            XmkExport mid = new XmkExport(xmk);
            mid.Export(args[1]);
        }
    }
}
