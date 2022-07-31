using CSharpSandbox.Wpf.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace CSharpSandbox.Wpf.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IServiceProvider Services { get; }

        public MainWindow(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));

            InitializeComponent();

            Task.Factory.StartNew(Terminal.Start);
        }

        private void Self_Closing(object sender, CancelEventArgs e)
        {

        }

        private void Self_Closed(object sender, EventArgs e)
        {
            Terminal.Exit();
        }
    }
}
