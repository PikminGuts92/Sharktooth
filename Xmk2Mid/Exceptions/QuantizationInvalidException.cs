using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xmk2Mid.Exceptions
{
    public class QuantizationInvalidException : Exception
    {
        public QuantizationInvalidException(string value) : base($"Quantization of \"{value}\" is invalid. Expected ratio or decimal between 0 and 1.")
        {

        }
    }
}
