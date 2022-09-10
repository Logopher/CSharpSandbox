using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace CSharpSandbox.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static App? _current;

        public static new App Current
        {
            get => _current ?? throw new Exception();
            private set
            {
                if (_current != null)
                {
                    throw new Exception();
                }
                _current = value;
            }
        }
        public App()
        {
            Current = this;

            DispatcherUnhandledException += Self_DispatcherUnhandledException;

            InitializeComponent();
        }

        public void Self_Exit(object sender, ExitEventArgs e)
        {

        }

        void Self_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debugger.Break();
        }
    }
}
