using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Model
{
    public class MenuItem
    {
        readonly List<MenuItem> _children;

        internal Database.MenuItem Record { get; }

        public int? Id { get; private set; } = null;

        public MenuItem? Parent { get; private set; }

        public string Header
        {
            get => Record.Header;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException();
                }

                Record.Header = value;
            }
        }

        public char? AccessCharacter
        {
            get => Record.AccessCharacter;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException();
                }

                Record.AccessCharacter = value;
            }
        }

        public string? CommandName
        {
            get => Record.CommandName;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException();
                }

                Record.CommandName = value;
            }
        }

        public bool IsReadOnly
        {
            get => Record.IsReadOnly;
            private set
            {
                Record.IsReadOnly = value;
            }
        }

        public IReadOnlyList<MenuItem> Children => _children;

        public MenuItem(MenuItem? parent, string header, char? accessCharacter, string? commandName, bool isReadOnly, params MenuItem[] children)
        {
            Record = new Database.MenuItem
            {
                Parent = parent?.Record,
                Header = header,
                AccessCharacter = accessCharacter,
                CommandName = commandName,
                IsReadOnly = isReadOnly,
                Children = children
                    .Select(i => i.Record)
                    .ToList(),
            };

            // We could use :this() to call the constructor taking only a record, except
            // it would make new parent and child objects rather than reusing these.
            // This seems preferable to immediately destroying and replacing everything.
            Parent = parent;

            _children = children.ToList();
            foreach (var child in _children)
            {
                child.Parent = this;
            }
        }

        public MenuItem(MenuItem? parent, string header, char? accessCharacter, string? commandName, params MenuItem[] children)
            : this(parent, header, accessCharacter, commandName, false, children)
        {
        }

        public MenuItem(string header, char? accessCharacter, string? commandName, bool isReadOnly, params MenuItem[] children)
            : this(null, header, accessCharacter, commandName, isReadOnly, children)
        {
        }

        public MenuItem(string header, char? accessCharacter, string? commandName, params MenuItem[] children)
            : this(null, header, accessCharacter, commandName, false, children)
        {
        }

        public MenuItem(MenuItem? parent, string header, char? accessCharacter, bool isReadOnly, params MenuItem[] children)
            : this(parent, header, accessCharacter, null, false, children)
        {
        }

        public MenuItem(MenuItem? parent, string header, char? accessCharacter, params MenuItem[] children)
            : this(parent, header, accessCharacter, null, false, children)
        {
        }

        public MenuItem(string header, char? accessCharacter, bool isReadOnly, params MenuItem[] children)
            : this(null, header, accessCharacter, null, isReadOnly, children)
        {
        }

        public MenuItem(string header, char? accessCharacter, params MenuItem[] children)
            : this(null, header, accessCharacter, null, false, children)
        {
        }

        public MenuItem(MenuItem? parent, string header, bool isReadOnly, params MenuItem[] children)
            : this(parent, header, null, null, false, children)
        {
        }

        public MenuItem(MenuItem? parent, string header, params MenuItem[] children)
            : this(parent, header, null, null, false, children)
        {
        }

        public MenuItem(string header, bool isReadOnly, params MenuItem[] children)
            : this(null, header, null, null, isReadOnly, children)
        {
        }

        public MenuItem(string header, params MenuItem[] children)
            : this(null, header, null, null, false, children)
        {
        }

        internal MenuItem(Database.MenuItem record)
        {
            Record = record;

            if (record.Parent != null)
            {
                Parent = new MenuItem(record.Parent);
            }

            _children = record.Children
                .Select(i => new MenuItem(i))
                .ToList();
        }

        public MenuItem(MenuItem? parent, string header, string commandName, bool isReadOnly = false)
            : this(parent, header, null, commandName, isReadOnly)
        {
        }

        public void Add(MenuItem child)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException();
            }

            _children.Add(child);
            Record.Children.Add(child.Record);
        }

        internal void Remove(MenuItem child)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException();
            }

            _children.Remove(child);
            Record.Children.Remove(child.Record);
        }
    }
}
