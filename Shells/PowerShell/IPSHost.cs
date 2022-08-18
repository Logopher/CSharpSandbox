using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Shells.PowerShell
{
    internal interface IPSHost
    {
        CultureInfo CurrentCulture { get; }

        CultureInfo CurrentUICulture { get; }

        Guid InstanceId { get; }

        string Name { get; }

        PSHostUserInterface UI { get; }

        Version Version { get; }

        void EnterNestedPrompt();

        void ExitNestedPrompt();

        void NotifyBeginApplication();

        void NotifyEndApplication();

        void SetShouldExit(int exitCode);
    }
}
