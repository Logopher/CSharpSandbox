using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace CSharpSandbox.Shells.PowerShell
{
    internal class PSHost : System.Management.Automation.Host.PSHost, IPSHost
    {
        private readonly ITerminal _terminal;

        public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

        public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

        public override Guid InstanceId { get; } = Guid.NewGuid();

        public override string Name { get; }

        public override PSHostUserInterface UI { get; }

        public override Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? throw new Exception();

        public Runspace Runspace { get; internal set; }

        public PSHost(string name, ITerminal terminal)
        {
            UI = new UIProxy(terminal);
            Name = name;
            _terminal = terminal;
            Runspace = RunspaceFactory.CreateRunspace(this, InitialSessionState.CreateDefault());
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public override void SetShouldExit(int exitCode) => _terminal.Exit(exitCode);
    }
}
