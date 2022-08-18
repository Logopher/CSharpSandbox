using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using CSharpSandbox.PSHell.View;
using System.IO;

namespace CSharpSandbox.PSHell;

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
            Utilities.StaThreadWrapper(async () =>
            {
                App? app = null;
                MainWindow? window = null;
                try
                {
                    using var host = CreateHostBuilder(args).Build();
                    await host.StartAsync();

                    app = host.Services.GetRequiredService<App>();

                    window = new MainWindow(host.Services);

                    app.Run(window);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    window?.Close();
                    app?.Shutdown();
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