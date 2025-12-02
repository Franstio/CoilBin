using Avalonia;
using Avalonia.Controls;
using CoilBin.Extensions;
using CoilBin.PLC;
using CoilBin.PLC.Extension;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoilBin.Desktop;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    private static Task WebApiTask = null!;
    private static WebApplication app = null!;
    [STAThread]
    public static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.Console())
            .WriteTo.Async(a => a.File("logs/app.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7))
            .WriteTo.Debug() // shows in VS output window
            .CreateLogger();
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/bin/bash", Arguments = "-c \"sudo systemctl stop backend-web\"", };
            Process proc = new Process() { StartInfo = startInfo, };
            proc.Start();
        }
        catch (Exception ex) { Log.Error(ex.Message); }
        ServicesLocator.ServiceCollection.BuildConfig();
        ServicesLocator.ServiceCollection.AddCommonServices();
        ServicesLocator.BuildServices();

        var configWeb = CoilBin.WebApi.Program.ConfigWeb(args);
        ServicesLocator.ServiceCollection.BuildConfig();
        ServicesLocator.ServiceCollection.AddCommonServices();

        foreach (var service in ServicesLocator.ServiceCollection)
            configWeb.Services.Add(service);
        ServicesLocator.BuildServices();

        var papp = CoilBin.WebApi.Program.BuildWeb(configWeb);

        WebApiTask = Task.Run(  async ()=>
        {
            try
            {
                app = await papp;
                await app.RunAsync("http://*:5000");
            }
            catch(Exception e)
            {
                Log.Error(e.Message);
            }
        });
        
        var builder = BuildAvaloniaApp();
        if(args.Contains("--drm"))
        {
           SilenceConsole();
                
            // If Card0, Card1 and Card2 all don't work. You can also try:                 
            // return builder.StartLinuxFbDev(args);
            // return builder.StartLinuxDrm(args, "/dev/dri/card1");
            return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
        }

        return builder.StartWithClassicDesktopLifetime(args, async (lifetime) =>
        {
            lifetime.Exit += async (s, e) =>
            {
                if (app is not null)
                {
                    await app.StopAsync();
                    await app.DisposeAsync();
                }
            };
        });
    }


    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();


    private static void SilenceConsole()
    {
        new Thread(() =>
            {
                Console.CursorVisible = false;
                while(true)
                    Console.ReadKey(true);
            })
            { IsBackground = true }.Start();
    }
}
