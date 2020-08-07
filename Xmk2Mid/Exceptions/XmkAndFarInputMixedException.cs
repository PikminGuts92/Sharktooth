using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xmk2Mid.Exceptions
{
    public class XmkAndFarInputMixedException : Exception
    {
        public XmkAndFarInputMixedException()
            : base("Mix of FAR and XMK paths given. Expected file extension type.")
        {

        }
    }
}
