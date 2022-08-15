using System;

namespace CSharpSandbox.Wpf.Shells
{
    internal class Line
    {
        public bool EndOfContent { get; private set; }

        public object Token { get; private set; }

        public DateTime Time { get; private set; }

        public string Text { get; private set; }

        public Line(object token, bool isEnd, DateTime time, string text)
        {
            Token = token;
            EndOfContent = isEnd;
            Time = time;
            Text = text;
        }
    }
}