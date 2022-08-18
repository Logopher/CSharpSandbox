using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Shells;

public interface ITerminal
{
    string? ReadLine();

    void Write(string value);

    void WriteLine(string line);

    void Exit(int exitCode);
}
