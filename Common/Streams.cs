using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public static class Streams
    {
        public static IEnumerable<Line> ReadLines(StreamReader reader)
        {
            var lines = new List<string>();

            var buffer = new StringBuilder();

            while (-1 < reader.Peek())
            {
                var now = DateTime.Now;

                {
                    var tempBuffer = new char[1024];

                    var read = reader.Read(tempBuffer, 0, tempBuffer.Length);

                    buffer.Append(tempBuffer, 0, read);
                }

                var bufferContents = buffer.ToString();

                lines.AddRange(bufferContents.Split(Environment.NewLine));
                buffer.Clear();

                if (lines.Count == 0)
                {
                    continue;
                }

                foreach (var line in lines)
                {
                    yield return new Line(reader, now, line + Environment.NewLine);
                }

                lines.Clear();
            }
        }
        public class Line
        {
            public object Token { get; private set; }

            public DateTime Time { get; private set; }

            public string Text { get; private set; }

            public Line(object token, DateTime time, string text)
            {
                Token = token;
                Time = time;
                Text = text;
            }
        }
    }
}
