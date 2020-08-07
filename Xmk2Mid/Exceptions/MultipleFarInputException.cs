using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xmk2Mid.Exceptions
{
    public class MultipleFarInputException : Exception
    {
        public MultipleFarInputException() : base("More than 1 FAR archive path given. Expected just one path.")
        {

        }
    }
}
