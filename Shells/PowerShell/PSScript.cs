using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Shells.PowerShell;

using PS = System.Management.Automation.PowerShell;

internal class PSScript : Script
{
    public override Language Language { get; } = Language.PowerShell;

    internal override string Source { get; }

    public PSScript(string source)
    {
        Source = source;
    }
}
