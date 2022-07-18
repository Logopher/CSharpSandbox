using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using CSharpSandbox.Wpf.View;

namespace CSharpSandbox.Wpf;

public class Program
{
    static IHostBuilder CreateHostBuilder(string[] args)
    {
        var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<App>()
                    .AddTransient<IDaq, Daq>()
                    .AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                        loggingBuilder.AddNLog(config);
                    });
            });
    }

    public static int Main(string[] args)
    {
        Utilities.StaThreadWrapper(async () =>
        {
            try
            {
                using var host = CreateHostBuilder(args).Build();
                await host.StartAsync();

                var app = host.Services.GetRequiredService<App>();
                //app.InitializeComponent();
                app.Run(new MainWindow(host.Services));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });

        return 0;
    }
}