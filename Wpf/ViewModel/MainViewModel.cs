using CSharpSandbox.Common;
using CSharpSandbox.Wpf.Gestures;
using CSharpSandbox.Wpf.Infrastructure;
using CSharpSandbox.Wpf.View;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CSharpSandbox.Wpf.ViewModel
{
    public class MainViewModel : RootViewModel
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
