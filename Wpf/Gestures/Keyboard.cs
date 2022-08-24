using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CSharpSandbox.Wpf.Gestures
{
    public static class Keyboard
    {
        private static readonly IReadOnlyDictionary<Key, string> _canonicalNames = new Dictionary<Key, string>
        {
            {Key.Enter, nameof(Key.Enter)},
            {Key.KanaMode, nameof(Key.KanaMode)},
            {Key.KanjiMode, nameof(Key.KanjiMode)},
            {Key.PageUp, nameof(Key.PageUp)},
            {Key.PageDown, nameof(Key.PageDown)},
            {Key.PrintScreen, nameof(Key.PrintScreen)},
            {Key.OemSemicolon, nameof(Key.OemSemicolon)},
            {Key.OemQuestion, nameof(Key.OemQuestion)},
            {Key.OemTilde, nameof(Key.OemTilde)},
            {Key.OemOpenBrackets, nameof(Key.OemOpenBrackets)},
            {Key.OemPipe, nameof(Key.OemPipe)},
            {Key.OemCloseBrackets, nameof(Key.OemCloseBrackets)},
            {Key.OemQuotes, nameof(Key.OemQuotes)},
            {Key.OemBackslash, nameof(Key.OemBackslash)},
            {Key.OemAttn, nameof(Key.OemAttn)},
            {Key.OemFinish, nameof(Key.OemFinish)},
            {Key.OemCopy, nameof(Key.OemCopy)},
            {Key.OemAuto, nameof(Key.OemAuto)},
            {Key.OemEnlw, nameof(Key.OemEnlw)},
            {Key.OemBackTab, nameof(Key.OemBackTab)},
            {Key.Attn, nameof(Key.Attn)},
            {Key.CrSel, nameof(Key.CrSel)},
            {Key.ExSel, nameof(Key.ExSel)},
            {Key.EraseEof, nameof(Key.EraseEof)},
            {Key.Play, nameof(Key.Play)},
            {Key.Zoom, nameof(Key.Zoom)},
            {Key.NoName, nameof(Key.NoName)},
            {Key.Pa1, nameof(Key.Pa1)},
        };

        public static readonly IReadOnlyDictionary<string, Key> AllKeys = Enum
            .GetNames(typeof(Key))
            .Cast<string>()
            .ToDictionary(s => s, s => Enum.Parse<Key>(s));

        public static readonly IReadOnlyDictionary<Key, IReadOnlyList<string>> AllKeyNames = AllKeys
            .GroupBy(kv => kv.Value)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var names = g.Select(kv => kv.Key).OrderBy(s => s).ToList();
                    return (IReadOnlyList<string>)names;
                });

        public static IList<string> GetNames(Key key) => AllKeyNames[key].ToList();

        public static string GetCanonicalName(Key key)
        {
            if (_canonicalNames.TryGetValue(key, out var name))
            {
                return name;
            }
            else
            {
                var names = GetNames(key);
                Debug.Assert(names.Count == 1);
                var result = names.FirstOrDefault();
                Debug.Assert(result != null);
                return result;
            }
        }
    }
}
