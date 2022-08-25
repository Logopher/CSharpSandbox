using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public static class Mundane
    {
        public const string EmptyString = "";

        public static T? Step<T>(this IEnumerator<T> enumer)
            where T : class
        {
            var result = enumer.MoveNext();
            return result ? enumer.Current : null;
        }

        public static bool Step<T>(this IEnumerator<T> enumer, [NotNullWhen(true)] out T? value)
        {
            var result = enumer.MoveNext();
            value = result ? enumer.Current : default;
            return result;
        }
    }
}
