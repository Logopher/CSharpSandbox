using System.Runtime.InteropServices;

namespace CSharpSandbox.HelloWorld
{
    internal class Class1
    {
        [UnmanagedCallersOnly(EntryPoint = "Answer")]
        public static int Answer()
        {
            return 41;
        }
    }
}
