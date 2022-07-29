using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Wpf.Shells
{
    internal class PSDriver : IShellDriver
    {
        public bool HasStarted => throw new NotImplementedException();

        public bool HasExited => throw new NotImplementedException();

        public string FullPrompt => throw new NotImplementedException();

        public async Task Start(Action<string, bool> print)
        {
            throw new NotImplementedException();
        }

        public async Task Execute(string command)
        {
            throw new NotImplementedException();
        }

        public void StopExecution()
        {
            throw new NotImplementedException();
        }

        public void End()
        {
            throw new NotImplementedException();
        }
    }
}
