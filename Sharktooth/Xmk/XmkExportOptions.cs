using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth.Xmk
{
    public struct XmkExportOptions
    {
        public decimal Quantization;
        public bool Remap;

        public readonly static XmkExportOptions Default = new XmkExportOptions() { Quantization = 1 / 128.0M, Remap = false };
    }
}
