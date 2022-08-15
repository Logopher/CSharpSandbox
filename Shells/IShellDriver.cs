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