using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public static class Mundane
    {
        public const string EmptyString = "";

        public static bool Step<T>(this IEnumerator<T> enumer, out T? value)
        {
            var result = enumer.MoveNext();
            value = result ? enumer.Current : default;
            return result;
        }
    }
}
