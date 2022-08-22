namespace CSharpSandbox.Shells;

public abstract class ShellDriver
{
    protected ITerminal Terminal { get; }

    public abstract bool HasStarted { get; protected set; }

    public abstract bool HasExited { get; protected set; }

    public abstract bool IsExecuting { get; protected set; }

    public abstract bool IsReadyForInput { get; protected set; }

    public abstract bool IsInSameProcess { get; protected set; }

    public string FullPrompt { get; } = "> ";

    public string PromptTemplate { get; }

    public ShellDriver(ITerminal terminal, string promptTemplate)
    {
        Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        PromptTemplate = promptTemplate ?? throw new ArgumentNullException(nameof(promptTemplate));
    }

    public abstract Task Start();

    public abstract Task Execute(string command);

    public abstract Task StopExecution();

    public abstract Task End();

    public string? ReadLine()
    {
        IsReadyForInput = true;
        var result = Terminal.ReadLine();
        IsReadyForInput = false;
        return result;
    }

    public void Write(object? value) => Terminal.Write(value);

    public void WriteLine(object? value) => Terminal.WriteLine(value);
}