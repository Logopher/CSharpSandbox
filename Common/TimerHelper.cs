using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common;

public class TimerHelper : IDisposable
{
    private Timer? _timer;
    private readonly object _threadLock = new();

    public bool HasStarted { get; private set; }
    public bool HasCompleted { get; private set; }
    public bool IsRunning { get; private set; }
    public TimeSpan Period { get; private set; }
    public DateTime StartTime { get; private set; }
    public TimeSpan Duration { get; private set; }
    public DateTime DueTime { get; private set; }

    public event Action<Timer, object?>? TimerEvent;

    public void Start(TimeSpan duration, TimeSpan period, bool triggerAtStart = false, object? state = null)
    {
        HasStarted = true;
        IsRunning = true;
        HasCompleted = false;
        Period = period;
        StartTime = DateTime.Now;
        Duration = duration;
        DueTime = DateTime.Now + duration;

        if (_timer != null)
        {
            Stop();
        }
        _timer = new Timer(
            Timer_Elapsed,
            state,
            triggerAtStart ? TimeSpan.FromTicks(0) : duration,
            period);
    }

    public void Stop() => Stop(TimeSpan.FromMinutes(2));

    public void Stop(TimeSpan timeout)
    {
        if (_timer == null)
        {
            throw new InvalidOperationException("Attempted to stop a timer which was not running.");
        }

        // Wait for timer queue to be emptied, before we continue
        // (Timer threads should have left the callback method given)
        // - http://woowaabob.blogspot.dk/2010/05/properly-disposing-systemthreadingtimer.html
        // - http://blogs.msdn.com/b/danielvl/archive/2011/02/18/disposing-system-threading-timer.aspx
        lock (_threadLock)
        {
            ManualResetEvent waitHandle = new ManualResetEvent(false);
            if (_timer.Dispose(waitHandle))
            {
                // Timer has not been disposed by someone else
                if (!waitHandle.WaitOne(timeout))
                    throw new TimeoutException("Timeout waiting for timer to stop");
            }
            IsRunning = false;
            HasCompleted = true;
            waitHandle.Close();   // Only close if Dispose has completed succesful
            _timer = null;
        }
    }

    public void Dispose()
    {
        Stop();
        TimerEvent = null;
    }

    void Timer_Elapsed(object? state)
    {
        // Ensure that we don't have multiple timers active at the same time
        // - Also prevents ObjectDisposedException when using Timer-object
        //   inside this method
        // - Maybe consider to use _timer.Change(interval, Timeout.Infinite)
        //   (AutoReset = false)
        if (Monitor.TryEnter(_threadLock))
        {
            try
            {
                if (_timer == null)
                {
                    return;
                }

                TimerEvent?.Invoke(_timer, state);
            }
            finally
            {
                Monitor.Exit(_threadLock);
            }
        }
    }
}
