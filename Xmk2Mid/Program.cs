using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharktooth;

namespace Xmk2Mid
{
    class Program
    {
        static void Main(string[] args)
        {
            XMK xmk = XMK.FromFile(args[0]);
            XMKExport mid = new XMKExport(xmk);
            mid.Export(args[1]);
        }
    }
}
