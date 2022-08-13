using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CSharpSandbox.Wpf.Gestures
{

    public class InputGestureTree
    {
        readonly Dictionary<Stimulus, INode> _root = new Dictionary<Stimulus, INode>();

        public Walker Walk(Stimulus stimulus, bool createNodes = false)
        {
            if (!_root.TryGetValue(stimulus, out INode? node))
            {
                if (createNodes)
                {
                    node = new Branch(stimulus);
                    _root.Add(stimulus, node);
                }
                else
                {
                    throw new Exception();
                }
            }

            return new Walker(node, createNodes);
        }

        public ICommand GetCommand(params Stimulus[] stimuli)
        {
            var stim = stimuli[0];
            Branch? branch = null;

            for (var i = 1; i < stimuli.Length; i++)
            {
                if (!(branch ?? _root).TryGetValue(stim, out INode? node))
                {
                    throw new Exception();
                }

                stim = stimuli[i];

                if (node is Leaf leaf)
                {
                    if (i + 1 < stimuli.Length)
                    {
                        throw new Exception();
                    }

                    return leaf.Command;
                }
                else if (node is Branch temp)
                {
                    branch = temp;
                }
                else
                {
                    throw new Exception();
                }
            }

            throw new Exception();
        }

        public void SetCommand(ICommand command, params Stimulus[] stimuli)
        {
            if (stimuli.Length == 0)
            {
                throw new Exception();
            }

            var stim = stimuli[0];
            Branch? branch = null;

            for (var i = 1; i < stimuli.Length; i++)
            {
                if (!(branch ?? _root).TryGetValue(stim, out INode? node))
                {
                    throw new Exception();
                }

                stim = stimuli[i];

                if (node is Leaf leaf)
                {
                    if (i + 1 < stimuli.Length)
                    {
                        throw new Exception();
                    }

                    leaf.Command = command;

                    return;
                }
                else if (node is Branch temp)
                {
                    branch = temp;
                }
                else
                {
                    if (i + 1 < stimuli.Length)
                    {
                        temp = new Branch(stim);
                        branch![stim] = temp;
                        branch = temp;
                    }
                    else if (i + 1 == stimuli.Length)
                    {
                        leaf = new Leaf(stim, command);
                        branch![stim] = leaf;
                        return;
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }

            throw new Exception();
        }

        internal interface INode
        {
            Stimulus Stimulus { get; }
        }

        class Leaf : INode
        {
            public Stimulus Stimulus { get; }

            public ICommand Command { get; set; }

            public Leaf(ModifierKeys modifierKeys, Key key, ICommand command)
                : this(new(modifierKeys, key), command)
            {
            }

            public Leaf(Stimulus stim, ICommand command)
            {
                Stimulus = stim;
                Command = command;
            }
        }

        class Branch : Dictionary<Stimulus, INode>, INode
        {
            public Stimulus Stimulus { get; }

            public Branch(ModifierKeys modifierKeys, Key key)
                : this(new(modifierKeys, key))
            {
            }

            public Branch(Stimulus stim)
            {
                Stimulus = stim;
            }
        }

        public class Walker
        {
            Stimulus? _stimulus;
            Branch? _previousBranch;
            INode? _node;
            readonly bool _createNodes;

            public IList<Stimulus> RegisteredStimuli => _node is Branch b ? b.Keys.ToList() : throw new InvalidOperationException();

            public IList<Stimulus> Breadcrumbs { get; } = new List<Stimulus>();

            public ICommand Command
            {
                get => _node is Leaf l ? l.Command : throw new InvalidOperationException();
                set
                {
                    if (_node is Leaf l)
                    {
                        l.Command = value;
                    }
                    if (_node == null)
                    {
                        if (!_createNodes)
                        {
                            throw new InvalidOperationException();
                        }

                        if (_stimulus == null || _previousBranch == null)
                        {
                            throw new Exception();
                        }

                        var stim = _stimulus.GetValueOrDefault();
                        var temp = new Leaf(stim, value);
                        _previousBranch.Add(stim, temp);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            public bool IsLeaf => _node is Leaf;

            internal Walker(INode node, bool createNodes)
            {
                _node = node;
                _createNodes = createNodes;

                Breadcrumbs.Add(node.Stimulus);
            }

            public bool Walk(Stimulus stimulus)
            {
                if (_node is Branch b)
                {
                    if (b.TryGetValue(stimulus, out INode? temp))
                    {
                        _node = temp;
                    }
                    else
                    {
                        if (!_createNodes)
                        {
                            return false;
                        }

                        _stimulus = stimulus;
                    }

                    Breadcrumbs.Add(stimulus);

                    _previousBranch = b;

                    return true;
                }
                else if (_node is Leaf l)
                {
                    throw new InvalidOperationException();
                }
                else if (_node == null)
                {
                    if (!_createNodes)
                    {
                        throw new InvalidOperationException();
                    }

                    if (_stimulus == null || _previousBranch == null)
                    {
                        throw new Exception();
                    }

                    var stim = _stimulus.GetValueOrDefault();
                    var temp = new Branch(stim);
                    _previousBranch.Add(stim, temp);
                    _previousBranch = temp;

                    Breadcrumbs.Add(stimulus);

                    return true;
                }

                throw new Exception();
            }
        }

        public struct Stimulus
        {
            public ModifierKeys ModifierKeys { get; }
            public Key Key { get; }

            public Stimulus(ModifierKeys modifiers, Key key)
            {
                ModifierKeys = modifiers;
                Key = key;
            }

            public override string ToString()
            {
                var keys = new List<string>();

                if (ModifierKeys.HasFlag(ModifierKeys.Control))
                { keys.Add("Ctrl"); }

                if (ModifierKeys.HasFlag(ModifierKeys.Alt))
                { keys.Add("Alt"); }

                if (ModifierKeys.HasFlag(ModifierKeys.Shift))
                { keys.Add("Shift"); }

                if (ModifierKeys.HasFlag(ModifierKeys.Windows))
                { keys.Add("Windows"); }

                keys.Add(Keyboard.GetCanonicalName(Key));

                return string.Join("+", keys);
            }

            public override bool Equals([NotNullWhen(true)] object? obj)
            {
                return obj != null
                    && obj is Stimulus stim
                    && ModifierKeys == stim.ModifierKeys
                    && Key == stim.Key;
            }

            public override int GetHashCode() => HashCode.Combine(ModifierKeys, Key);
        }
    }
}
