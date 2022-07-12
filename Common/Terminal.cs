using System.Runtime.InteropServices;
using System.Text;

namespace CSharpSandbox.Common
{
    public class Terminal
    {
        public Terminal()
        {
            bool openedConsole = (0 != Windows.AllocConsole());
            if (!openedConsole)
            {
                throw new Exception();
            }
        }
    }
}