using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Wpf.ViewModel
{
    public class AboutViewModel
    {
        private AssemblyName _assemblyName = Assembly.GetExecutingAssembly().GetName();
        public string AppName => _assemblyName.Name ?? "(application name unresolvable)";
        public string AppVersion => _assemblyName.Version?.ToString() ?? "? (version unresolvable)";
    }
}
