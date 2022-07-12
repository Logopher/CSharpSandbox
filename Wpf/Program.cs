using CSharpSandbox.Common;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CSharpSandbox.Wpf;

public class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        //_ = new Terminal();

        var w = new MainWindow();

        var app = new App();

        app.Run(w);

        return 0;
    }
}
