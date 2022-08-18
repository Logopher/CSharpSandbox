using CSharpSandbox.Common;
using CSharpSandbox.PSHell.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.PSHell.ViewModel
{
    internal class MainViewModel : RootViewModel
    {
        string _statusText = Mundane.EmptyString;
        string _gestureText = "placeholder";

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                NotifyPropertyChanged();
            }
        }
        public string GestureText
        {
            get => _gestureText;
            set
            {
                _gestureText = value;
                NotifyPropertyChanged();
            }
        }
    }
}
