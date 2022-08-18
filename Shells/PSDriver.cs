using CSharpSandbox.Shells.PowerShell;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using PSHost = CSharpSandbox.Shells.PowerShell.PSHost;

namespace CSharpSandbox.Shells;

public class PSDriver : ShellDriver, IPSHost
{
    private readonly PSHost _host;

    private System.Management.Automation.PowerShell? _powerShellCommand;

    public override bool HasStarted { get; protected set; }

    public override bool HasExited { get; protected set; }

    public string? CurrentDirectory { get; private set; }

    public CultureInfo CurrentCulture => _host.CurrentCulture;

    public CultureInfo CurrentUICulture => _host.CurrentUICulture;

    public Guid InstanceId => _host.InstanceId;

    public string Name { get; } = nameof(PSDriver);

    public PSHostUserInterface UI => _host.UI;

    public Version Version => _host.Version;

    public PSDriver(ITerminal terminal, string promptTemplate)
        : base(terminal, promptTemplate)
    {
        _host = new PSHost(Name, terminal);
    }

    public override Task Start(Action<string, bool> print)
    {
        HasStarted = true;

        Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        _host.Runspace.Open();

        return Task.CompletedTask;
    }

    public override Task Execute(string command)
    {
        try
        {
            _powerShellCommand = System.Management.Automation.PowerShell.Create();
            _powerShellCommand.AddScript(command);
            _powerShellCommand.Runspace = _host.Runspace;

            var results = _powerShellCommand.Invoke();

            // Display the results.
            foreach (PSObject result in results)
            {
                Print(result);
            }

            // Display any non-terminating errors.
            foreach (ErrorRecord error in _powerShellCommand.Streams.Error)
            {
                Console.WriteLine("PowerShell Error: {0}", error);
            }
        }
        catch (RuntimeException ex)
        {
            Console.WriteLine("PowerShell Error: {0}", ex.Message);
            Console.WriteLine();
        }

        return Task.CompletedTask;
    }

    public override Task StopExecution()
    {
        _powerShellCommand?.Stop();

        return Task.CompletedTask;
    }

    public override Task End()
    {
        _host.Runspace.Close();

        _powerShellCommand?.Dispose();
        _powerShellCommand = null;
        HasExited = true;
        HasStarted = false;

        return Task.CompletedTask;
    }

    public void EnterNestedPrompt() => _host.EnterNestedPrompt();

    public void ExitNestedPrompt() => _host.ExitNestedPrompt();

    public void NotifyBeginApplication() => _host.NotifyBeginApplication();

    public void NotifyEndApplication() => _host.NotifyEndApplication();

    public void SetShouldExit(int exitCode) => _host.SetShouldExit(exitCode);

    public override void Print(object? message = null, bool newline = true)
    {
        throw new NotImplementedException();
    }
}
