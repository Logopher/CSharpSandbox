using System;

namespace CSharpSandbox.Wpf.Shells
{
    public delegate void ShellEventHandler(object sender, ShellEventArgs? e);

    public enum ShellEventType
    {
        Chunk,
        Character,
    }

    public class ShellEventArgs : EventArgs
    {
        private bool _breakChunk = false;

        public ShellEventType EventType { get; }
        public string ChunkBuffer { get; set; }
        public char? Character { get; }
        public bool BreakChunk
        {
            get => _breakChunk;
            set
            {
                if (EventType != ShellEventType.Character)
                {
                    throw new InvalidOperationException($"{nameof(ShellEventType.Chunk)} events can't break chunks.");
                }

                _breakChunk = value;
            }
        }

        public ShellEventArgs(ShellEventType eventType, string lineBuffer, char? character = null)
        {
            EventType = eventType;
            ChunkBuffer = lineBuffer;
            Character = character;
        }
    }
}