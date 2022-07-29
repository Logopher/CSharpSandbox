using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common;

public class TimerHelper : IDisposable
{
    private Timer? _timer;
    private TimeSpan? _stopTimeout;
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

        _stopTimeout = timeout;
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

                if (_stopTimeout != null)
                {
                    var waitHandle = new ManualResetEvent(false);
                    _timer.Dispose();
                    /*
                    if (_timer.Dispose(waitHandle))
                    {
                        // Timer has not been disposed by someone else
                        if (!waitHandle.WaitOne(_stopTimeout.Value))
                        {
                            throw new TimeoutException("Timeout waiting for timer to stop");
                        }
                    }
                    */
                    IsRunning = false;
                    HasCompleted = true;
                    waitHandle.Close();   // Only close if Dispose has completed succesful
                    _timer = null;
                }
            }
            finally
            {
                Monitor.Exit(_threadLock);
            }
        }
    }
}
