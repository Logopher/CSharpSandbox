using CSharpSandbox.Common;
using CSharpSandbox.Wpf.Infrastructure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSandbox.Wpf.ViewModel
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
