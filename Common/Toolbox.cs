using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Common
{
    public class Toolbox
    {
        static Toolbox? _instance;

        static Toolbox Instance
        {
            get => _instance ?? throw new Exception();
            set
            {
                if (_instance != null)
                {
                    throw new Exception();
                }
                _instance = value;
            }
        }

        public Toolbox()
        {
            Instance = this;
        }
    }
}
