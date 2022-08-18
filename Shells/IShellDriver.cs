namespace CSharpSandbox.Shells;

public interface IShellDriver
{
    bool HasStarted { get; }
    bool HasExited { get; }
    string FullPrompt { get; }

    Task Start(Action<string, bool> print);
    Task Execute(string command);
    Task StopExecution();
    Task End();
}

public abstract class ShellDriver : IShellDriver
{
    protected readonly ITerminal Terminal;

    public abstract bool HasStarted { get; protected set; }

    public abstract bool HasExited { get; protected set; }

    public string FullPrompt { get; } = "> ";

    public string PromptTemplate { get; }

    public ShellDriver(ITerminal terminal, string promptTemplate)
    {
        Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        PromptTemplate = promptTemplate ?? throw new ArgumentNullException(nameof(promptTemplate));
    }

    public abstract Task Start(Action<string, bool> print);

    public abstract Task Execute(string command);

    public abstract Task StopExecution();

    public abstract Task End();

    public abstract void Print(object? text = null, bool newline = true);
}