using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Shells;

public interface ITerminal
{
    string? ReadLine();

    void Write(object? value);

    void WriteLine(object? line = null);

    void Exit(int exitCode);
}
