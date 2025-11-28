using Avalonia;
using CoilBin.Desktop.Extension;
using CoilBin.PLC;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoilBin.Desktop;

public class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        ServicesLocator.ServiceCollection.BuildConfig();
        ServicesLocator.ServiceCollection.AddCommonServices();
        ServicesLocator.BuildServices();

        var configWeb = CoilBin.Api.Program.ConfigWeb(args);
        ServicesLocator.ServiceCollection.BuildConfig();
        ServicesLocator.ServiceCollection.AddCommonServices();
        ServicesLocator.BuildServices();
        configWeb.Host.UseServiceProviderFactory(new ServicesLocator.ExternalServiceProviderFactory(ServicesLocator.Services));


        var app = CoilBin.Api.Program.BuildWeb(configWeb);

        _ = Task.Run( async ()=>
        {
            await app.RunAsync("http://localhost:5000");
        });
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Async(a => a.Console())
            .WriteTo.Async(a => a.File("logs/app.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7))
            .WriteTo.Debug() // shows in VS output window
            .CreateLogger();
        var builder = BuildAvaloniaApp();
        if(args.Contains("--drm"))
        {
            SilenceConsole();
                
            // If Card0, Card1 and Card2 all don't work. You can also try:                 
            // return builder.StartLinuxFbDev(args);
            // return builder.StartLinuxDrm(args, "/dev/dri/card1");
            return builder.StartLinuxDrm(args, "/dev/dri/card1", 1D);
        }

        return builder.StartWithClassicDesktopLifetime(args);
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
