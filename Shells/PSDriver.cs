using CSharpSandbox.Common;
using CSharpSandbox.Shells.PowerShell;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using PSHost = CSharpSandbox.Shells.PowerShell.PSHost;

namespace CSharpSandbox.Shells;

public sealed class PSDriver : ShellDriver, IPSHost
{
    private readonly PSHost _host;

    private System.Management.Automation.PowerShell? _powerShellCommand;

    public override bool HasStarted { get; protected set; }

    public override bool HasExited { get; protected set; }

    public override bool IsReadyForInput { get; protected set; }

    public override bool IsInSameProcess { get; protected set; } = true;

    private readonly MemoryStream _input = new();

    private readonly MemoryStream _output = new();

    private readonly MemoryStream _error = new();

    public string? CurrentDirectory { get; private set; }

    public CultureInfo CurrentCulture => _host.CurrentCulture;

    public CultureInfo CurrentUICulture => _host.CurrentUICulture;

    public Guid InstanceId => _host.InstanceId;

    public string Name { get; } = nameof(PSDriver);

    public PSHostUserInterface UI => _host.UI;

    public Version Version => _host.Version;

    public override bool IsExecuting { get; protected set; }

    public PSDriver(ITerminal terminal, string promptTemplate)
        : base(terminal, promptTemplate)
    {
        _host = new PSHost(Name, terminal);
    }

    public override Task Start()
    {
        HasStarted = true;

        Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        _host.Runspace.Open();

        Write(FullPrompt);

        IsReadyForInput = true;

        return Task.CompletedTask;
    }

    public override Task Execute(string command)
    {
        try
        {
            if (IsExecuting)
            {
                var trueInput = Console.In;
                Console.SetIn(new StreamReader(_input));
                var inputWriter = new StreamWriter(_input);

                inputWriter.WriteLine(command);

                Console.SetIn(trueInput);

                return Task.CompletedTask;
            }

            IsExecuting = true;
            //IsReadyForInput = false;

            var trueOutput = Console.Out;
            var trueError = Console.Error;

            Console.SetOut(new StreamWriter(_output));
            Console.SetError(new StreamWriter(_error));

            var outputReader = new StreamReader(_output);
            var errorReader = new StreamReader(_error);

            var outputThread = new Thread(async () =>
            {
                while (!outputReader.EndOfStream)
                {
                    foreach (var line in Streams.ReadLines(outputReader))
                    {
                        WriteLine(line.Text);
                    }

                    await Task.Delay(25);
                }
            });

            var errorThread = new Thread(async () =>
            {
                while (!errorReader.EndOfStream)
                {
                    foreach (var line in Streams.ReadLines(errorReader))
                    {
                        WriteLine(line.Text);
                    }

                    await Task.Delay(25);
                }
            });

            var executionThread = new Thread(() =>
            {
                _powerShellCommand = System.Management.Automation.PowerShell.Create();
                _powerShellCommand.AddScript(command);
                _powerShellCommand.Runspace = _host.Runspace;

                var results = _powerShellCommand.Invoke();

                // Display the results.
                foreach (PSObject result in results)
                {
                    WriteLine(result);
                }

                // Display any non-terminating errors.
                foreach (ErrorRecord error in _powerShellCommand.Streams.Error)
                {
                    WriteLine($"PowerShell Error: {error}");
                }

                Write(FullPrompt);

                IsExecuting = false;
                IsReadyForInput = true;

                Console.SetOut(trueOutput);
                Console.SetError(trueError);
            });
            executionThread.Start();
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
}
