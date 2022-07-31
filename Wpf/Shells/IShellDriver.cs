using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Wpf.Shells
{
    internal interface IShellDriver
    {
        bool HasStarted { get; }
        bool HasExited { get; }
        string FullPrompt { get; }

        void Start(Action<string, bool> print);
        Task Execute(string command);
        void StopExecution();
        void End();
    }
}
