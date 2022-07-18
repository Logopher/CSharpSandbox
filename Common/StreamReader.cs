using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public class StreamReader : TextReader
    {
        private readonly StringBuilder _buffer = new();

        public Stream BaseStream { get; }
        public int CharacterBufferSize { get; }
        public int StringBufferContents => _buffer.Length;

        public StreamReader(Stream stream, int bufferSize = 100)
        {
            BaseStream = stream;
            CharacterBufferSize = bufferSize;
        }

        public override int Read()
        {
            var peek = Peek();

            if (peek == -1)
            {
                return peek;
            }

            return base.Read();
        }

        public string Read(bool flush = false)
        {
            var charBuffer = new char[CharacterBufferSize];
            int readCount = Read(charBuffer, 0, CharacterBufferSize);

            if (_buffer.Length == 0 && readCount == 0)
            {
                return string.Empty;
            }

            if (readCount != 0)
            {
                _buffer.Append(charBuffer[..readCount]);
            }

            var str = _buffer.ToString();

            var newline = str.IndexOf(Environment.NewLine);

            if (newline == -1)
            {
                if (flush || readCount < CharacterBufferSize)
                {
                    _buffer.Clear();
                    return str;
                }

                return string.Empty;
            }

            var afterNewline = newline + Environment.NewLine.Length;

            var result = str[..afterNewline];
            _buffer.Remove(0, afterNewline);
            return result;
        }
    }
}
