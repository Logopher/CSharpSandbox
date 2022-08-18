﻿using CSharpSandbox.Common;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CSharpSandbox.Wpf.Infrastructure
{
    internal abstract class BaseViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string name = Mundane.EmptyString)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
