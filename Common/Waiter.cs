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
            _tcs.SetResult(false);
            _timer.Stop();
        }
    }

    public Task<bool> Wait(int duration, int period, Func<bool> poll)
    {
        _timer = new();
        _tcs = new();

        _timer.TimerEvent += (_, _) =>
        {
            if (_timer.HasCompleted)
            {
                return;
            }

            if (!poll())
            {
                _tcs.SetResult(false);
                _timer.Stop();
            }
            else if (_timer.DueTime - _timer.Period < DateTime.Now)
            {
                _tcs.SetResult(true);
                _timer.Stop();
            }

            string.Empty.ToString();
        };

        _timer.Start(TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(period));

        return _tcs.Task;
    }
}