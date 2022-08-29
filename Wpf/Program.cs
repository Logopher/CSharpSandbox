using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using CSharpSandbox.Wpf.View;
using System.IO;
using CSharpSandbox.Wpf.ViewModel;
using CSharpSandbox.Wpf.Gestures;
using System.Windows;
using Data.Database;
using Data;
using System.Diagnostics;

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
                services.AddSingleton<App>();
                services.AddTransient<IDaq, Daq>();
                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                    loggingBuilder.AddNLog(config);
                });

                services.AddSingleton<Context>();

                services.AddSingleton<Repository>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<AboutWindow>();

                services.AddSingleton<MainViewModel>();
                services.AddSingleton<AboutViewModel>();

                services.AddSingleton<InputGestureTree>();
            });
    }

    public static int Main(string[] args)
    {
        int statusCode = 0;

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

                    var logger = host.Services.GetRequiredService<ILogger<Program>>();

                    try
                    {
                        app = host.Services.GetRequiredService<App>();

                        app.ShutdownMode = ShutdownMode.OnMainWindowClose;

                        app.MainWindow = host.Services.GetRequiredService<MainWindow>();

                        app.Run(app.MainWindow);
                    }
                    catch(Exception e)
                    {
                        Debugger.Break();
                        Console.WriteLine("Uncaught exception in application. Check log file.");
                        logger.LogError(e, "{Message}", e.Message);
                    }
                }
                catch (Exception e)
                {
                    Debugger.Break();
                    Console.WriteLine("DI facility failed.");
                    Console.WriteLine(e);
                    window?.Close();
                    app?.Shutdown();

                    statusCode = 1;
                }
            });
        }
        catch (Exception e)
        {
            Debugger.Break();
            Console.WriteLine("Thread facility failed.");
            Console.WriteLine(e);

            statusCode = 2;
        }

        return statusCode;
    }
}