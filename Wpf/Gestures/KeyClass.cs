using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CSharpSandbox.Wpf.Gestures
{
    public class KeyClass : IReadOnlySet<Key>
    {
        public static readonly KeyClass AllKeys = FromRanges((Key.Cancel, Key.OemClear));
        public static readonly KeyClass LetterKeys = FromRanges((Key.A, Key.Z));
        public static readonly KeyClass FunctionKeys = FromRanges((Key.F1, Key.F12));
        public static readonly KeyClass ExtendedFunctionKeys = FromRanges((Key.F13, Key.F24));
        public static readonly KeyClass NumericKeys = FromRanges((Key.D0, Key.D9), (Key.NumPad0, Key.NumPad9));
        public static readonly KeyClass ModifierKeys = FromRanges((Key.LWin, Key.RWin), (Key.LeftShift, Key.RightAlt));
        public static readonly KeyClass LockKeys = FromKeys(Key.NumLock, Key.Scroll, Key.CapsLock);
        public static readonly KeyClass NavigationKeys = FromRanges((Key.PageUp, Key.Down));
        public static readonly KeyClass WhitespaceKeys = FromKeys(Key.Enter, Key.Space, Key.Tab, Key.LineFeed);
        public static readonly KeyClass InputModeKeys = FromRanges((Key.KanaMode, Key.KanjiMode));
        public static readonly KeyClass ImeKeys = FromKeys(Key.ImeConvert, Key.ImeNonConvert, Key.ImeAccept, Key.ImeModeChange, Key.ImeProcessed);
        public static readonly KeyClass PunctuationKeys = FromRanges((Key.Multiply, Key.Divide));
        public static readonly KeyClass BrowserKeys = FromRanges((Key.BrowserBack, Key.BrowserHome));
        public static readonly KeyClass MediaKeys = FromRanges((Key.VolumeMute, Key.MediaPlayPause));
        public static readonly KeyClass OemKeys = FromRanges((Key.OemSemicolon, Key.OemBackslash), (Key.OemAttn, Key.OemBackTab), (Key.OemClear, Key.OemClear));
        public static readonly KeyClass MiscellaneousKeys = FromKeys(Key.Cancel, Key.Back, Key.Clear, Key.Pause, Key.Escape, Key.Select, Key.Print, Key.Execute, Key.PrintScreen, Key.Insert, Key.Delete, Key.Help, Key.Apps, Key.Sleep, Key.LaunchMail, Key.SelectMedia, Key.LaunchApplication1, Key.LaunchApplication2, Key.System, Key.Attn, Key.CrSel, Key.ExSel, Key.EraseEof, Key.Play, Key.Zoom, Key.NoName, Key.Pa1);
        public static readonly KeyClass UnaccountedKeys = AllKeys.Except(LetterKeys, FunctionKeys, ExtendedFunctionKeys, NumericKeys, ModifierKeys, LockKeys, NavigationKeys, WhitespaceKeys, InputModeKeys, ImeKeys, PunctuationKeys, BrowserKeys, MediaKeys, OemKeys, MiscellaneousKeys);

        readonly HashSet<Key> _keys = new();

        public int Count => throw new NotImplementedException();

        private KeyClass(IEnumerable<Key> keys)
        {
            _keys = keys.ToHashSet();
        }

        public static KeyClass FromKeys(params Key[] keys) => new KeyClass(keys);

        public static KeyClass FromRanges(params (Key, Key)[] ranges)
        {
            var keys = new List<Key>();

            foreach (var range in ranges)
            {
                if (range.Item2 < range.Item1)
                {
                    throw new Exception();
                }

                keys.AddRange(Enumerable.Range((int)range.Item1, range.Item2 - range.Item1 + 1).Cast<Key>());
            }

            return new KeyClass(keys);
        }

        public KeyClass Except(params KeyClass[] others)
        {
            var result = (IEnumerable<Key>)_keys;

            foreach (var other in others)
            {
                result = Enumerable.Except(result, other);
            }

            return new(result);
        }

        public KeyClass Union(params KeyClass[] others)
        {
            var result = (IEnumerable<Key>)_keys;

            foreach (var other in others)
            {
                result = Enumerable.Union(result, other);
            }

            return new(result);
        }

        public KeyClass Intersect(params KeyClass[] others)
        {
            var result = (IEnumerable<Key>)_keys;

            foreach (var other in others)
            {
                result = Enumerable.Intersect(result, other);
            }

            return new(result);
        }

        public IList<string> GetNames() => _keys.Select(k => Keyboard.GetCanonicalName(k)).ToList();

        public bool Contains(Key item) => _keys.Contains(item);

        public bool IsProperSubsetOf(IEnumerable<Key> other) => _keys.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<Key> other) => _keys.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<Key> other) => _keys.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<Key> other) => _keys.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<Key> other) => _keys.Overlaps(other);

        public bool SetEquals(IEnumerable<Key> other) => _keys.SetEquals(other);

        public IEnumerator<Key> GetEnumerator() => _keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _keys.GetEnumerator();
    }
}
