using Shells.PowerShell;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace CSharpSandbox.Shells;

public class PSDriver : PSHost, IShellDriver
{
    private PowerShell? _powerShellCommand;

    public bool HasStarted { get; private set; }

    public bool HasExited { get; private set; }

    public string FullPrompt { get; private set; }

    public string? CurrentDirectory { get; private set; }

    public override CultureInfo CurrentCulture { get; } = CultureInfo.CurrentCulture;

    public override CultureInfo CurrentUICulture { get; } = CultureInfo.CurrentUICulture;

    public override Guid InstanceId { get; } = Guid.NewGuid();

    public override string Name { get; } = typeof(PSDriver).Name;

    public override PSHostUserInterface UI { get; } = new UIProxy();

    public override Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version!;

    public Task Start(Action<string, bool> print)
    {
        HasStarted = true;

        Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        return Task.CompletedTask;
    }

    public Task Execute(string command)
    {
        try
        {
            _powerShellCommand = PowerShell.Create();
            _powerShellCommand.AddScript(command);
            _powerShellCommand.Runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault());
            _powerShellCommand.Runspace.Open();

            var results = _powerShellCommand.Invoke();

            _powerShellCommand.Runspace.Close();

            // Display the results.
            foreach (PSObject result in results)
            {
                Console.WriteLine(result);
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

    public Task StopExecution()
    {
        _powerShellCommand?.Stop();

        return Task.CompletedTask;
    }

    public Task End()
    {
        _powerShellCommand?.Dispose();
        _powerShellCommand = null;
        HasExited = true;
        HasStarted = false;

        return Task.CompletedTask;
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

    public override void SetShouldExit(int exitCode)
    {
        throw new NotImplementedException();
    }
}
