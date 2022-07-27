using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public class ShellStreamReader
    {
        private readonly Process _shellProcess;
        private readonly StreamReader _reader;
        private readonly Action<string, bool> _print;
        private readonly Waiter _waiter = new();
        private int? next = null;

        public bool IsIdle { get; private set; }

        public ShellStreamReader(Process shellProcess, StreamReader reader, Action<string, bool> print)
        {
            _shellProcess = shellProcess;
            _reader = reader;
            _print = print;
        }

        private int Peek() => Read(false);

        private int Read(bool consume)
        {
            var result = next ?? _reader.Read();
            if (result != -1)
            {
                IsIdle = false;
            }
            next = (consume || result == -1) ? null : result;
            Debug.WriteLine($"Read (consume: {consume}): {(result == -1 ? "-1" : (char)result)}");
            return result;
        }

        public void Read(Action<ShellEventArgs>? characterRead = null, Action<ShellEventArgs>? chunkRead = null)
        {
            if (_reader == null)
            {
                throw new ArgumentNullException(nameof(_reader));
            }

            string buffer = string.Empty;

            while (!(_shellProcess!.HasExited))
            {
                while (!buffer.EndsWith(Environment.NewLine))
                {
                    var next = (char)Read(true);

                    buffer += next;

                    var ev1 = new ShellEventArgs(ShellEventType.Character, buffer, next)
                    {
                        BreakChunk = buffer.EndsWith(Environment.NewLine)
                    };
                    characterRead?.Invoke(ev1);
                    buffer = ev1.ChunkBuffer;

                    if (ev1.BreakChunk)
                    {
                        break;
                    }
                }

                var ev2 = new ShellEventArgs(ShellEventType.Chunk, buffer);
                chunkRead?.Invoke(ev2);
                buffer = ev2.ChunkBuffer;

                _print(buffer, false);

                buffer = string.Empty;
            }
        }

        public void ProclaimIdle()
        {
            IsIdle = true;
            _waiter.Interrupt();
        }

        public async Task<bool> WaitForIdle()
        {
            bool result;
            while (result = await _waiter.Wait(50, 5, () =>
            {
                var peek = Peek();
                return peek == -1;
            }))
            {

            }
            IsIdle = result;

            return result;
        }
    }
}
