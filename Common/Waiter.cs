using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common;

public class Waiter
{
    TaskCompletionSource<bool>? _tcs;
    TimerHelper? _timer;

    public void Interrupt()
    {
        if (_timer == null || _tcs == null)
        {
            throw new InvalidOperationException("Interrupt was called before Start.");
        }

        if (_timer.IsRunning)
        {
            Stop(false);
        }
    }

    private void Stop(bool result)
    {
        if (_tcs == null || _timer == null)
        {
            throw new InvalidOperationException();
        }

        _tcs.SetResult(result);
        _timer.Stop();
        _tcs = null;
        _timer = null;
    }

    public Task<bool> Wait(int duration, int period, Func<bool> poll)
    {
        _timer = new();
        _tcs = new();

        _timer.TimerEvent += (_, _) =>
        {
            if (_timer?.HasCompleted ?? true)
            {
                return;
            }

            if (!poll())
            {
                Stop(false);
            }
            else if (_timer.DueTime - _timer.Period < DateTime.Now)
            {
                Stop(true);
            }
        };

        _timer.Start(TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(period));

        return _tcs.Task;
    }
}