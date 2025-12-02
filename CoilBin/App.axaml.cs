using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CoilBin.Extensions;
using CoilBin.Models;
using CoilBin.PLC;
using CoilBin.PLC.Contracts;
using CoilBin.PLC.Extension;
using CoilBin.ViewModels;
using CoilBin.Views;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text.Json;

namespace CoilBin;

public partial class App : Application
{

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        LiveCharts.Configure(config =>
        config
            .AddSkiaSharp()

    );

        BindingPlugins.DataValidators.RemoveAt(0);
        if (ServicesLocator.Services is null)
        {
            ServicesLocator.ServiceCollection.BuildConfig();
            ServicesLocator.ServiceCollection.AddCommonServices();
            ServicesLocator.BuildServices();
        }
        MainViewModel vm = ServicesLocator.Services.GetRequiredService<MainViewModel>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = vm
            };
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}

