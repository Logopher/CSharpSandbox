using CSharpSandbox.Shells.PowerShell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Shells;

public abstract class Script
{
    public abstract Language Language { get; }

    internal abstract string Source { get; }

    public static Script Create(Language language, string source)
    {
        switch (language)
        {
            case Language.CSharp:
                throw new NotImplementedException();
            case Language.PowerShell:
                return new PSScript(source);
            case Language.Batch:
                throw new NotImplementedException();
            default:
                throw new NotImplementedException();
        }
    }
}
