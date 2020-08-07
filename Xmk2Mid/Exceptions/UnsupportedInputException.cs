using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xmk2Mid.Exceptions
{
    public class UnsupportedInputException : Exception
    {
        public UnsupportedInputException(string inputPath)
            : base($"\"{inputPath}\" uses an unsupported file extension (only .far and .xmk are valid)")
        {

        }
    }
}
