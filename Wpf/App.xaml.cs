using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
        public App()
        {
            DispatcherUnhandledException += Self_DispatcherUnhandledException;
        }

        public void Self_Exit(object sender, ExitEventArgs e)
        {

        }

        void Self_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {

        }
    }
}
