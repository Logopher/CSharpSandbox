using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using CSharpSandbox.Wpf.View;
using System.IO;

namespace CSharpSandbox.Wpf;

public class Program
{
    static IHostBuilder CreateHostBuilder(string[] args)
    {
        var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) //From NuGet Package Microsoft.Extensions.Configuration.Json
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
        try
        {
            Utilities.StaThreadWrapper(async windowClosed =>
            {
                try
                {
                    using var host = CreateHostBuilder(args).Build();
                    await host.StartAsync();

                    var app = host.Services.GetRequiredService<App>();

                    var window = new MainWindow(host.Services);
                    window.Closed += windowClosed;

                    app.Run(window);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return 0;
    }
}