using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Model
{
    public class MenuItem
    {
        private readonly List<MenuItem> _children = new();
        private string _commandName;
        private char? _accessCharacter;
        private string _header;

        public string Header
        {
            get => _header;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException();
                }

                _header = value;
            }
        }

        public char? AccessCharacter
        {
            get => _accessCharacter;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException();
                }

                _accessCharacter = value;
            }
        }

        public string CommandName
        {
            get => _commandName;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException();
                }

                _commandName = value;
            }
        }

        public bool IsReadOnly { get; }

        public IReadOnlyList<MenuItem> Children => _children;

        public MenuItem(string header, char? accessCharacter, string commandName, bool isReadOnly = false)
        {
            _header = header;
            _accessCharacter = accessCharacter;
            _commandName = commandName;
            IsReadOnly = isReadOnly;
        }

        public MenuItem(string header, string commandName, bool isReadOnly = false)
            : this(header, null, commandName, isReadOnly)
        {
        }

        public void Add(MenuItem child)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException();
            }

            _children.Add(child);
        }
    }
}
