using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                try
                {
                    /*
                    SynchronizationContext.SetSynchronizationContext(
                        new DispatcherSynchronizationContext(
                            Dispatcher.CurrentDispatcher));
                    */
                    action();
                    //Dispatcher.Run();
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

        public static Task<bool> WaitForInterrupt()
        {
            TaskCompletionSource<bool> tcs = new();

            Task.Run(async () =>
            {
                await Task.Delay(50);

                tcs.SetResult(false);
            });

            return tcs.Task;
        }
    }
}
