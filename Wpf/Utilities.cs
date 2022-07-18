using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CSharpSandbox.Wpf
{
    public class Utilities
    {
        public static void StaThreadWrapper(Action action)
        {
            var t = new Thread(o =>
            {
                action();
                Dispatcher.Run();
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
