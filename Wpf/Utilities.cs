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
        public static void StaThreadWrapper(Action<EventHandler> action)
        {
            var t = new Thread(o =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(
                        new DispatcherSynchronizationContext(
                            Dispatcher.CurrentDispatcher));

                    action((o, e) =>
                    {
                        Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
                    });
                    Dispatcher.Run();
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw;
                }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
