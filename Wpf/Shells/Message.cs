using System;

namespace CSharpSandbox.Wpf.Shells
{
    internal class Message
    {
        public bool EndOfContent { get; private set; }

        public object Token { get; private set; }

        public DateTime Time { get; private set; }

        public string Text { get; private set; }

        public Message(object token, bool end, DateTime time, string text)
        {
            Token = token;
            EndOfContent = end;
            Time = time;
            Text = text;
        }
    }
}