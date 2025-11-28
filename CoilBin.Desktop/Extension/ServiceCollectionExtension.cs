using CoilBin.Models;
using CoilBin.PLC;
using CoilBin.PLC.Contracts;
using CoilBin.PLC.Services;
using CoilBin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoilBin.Desktop.Extension
{
    public static class ServiceCollectionExtension
    {
        public static void BuildConfig(this IServiceCollection collection)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "desktop_appsettings.json");
            ConfigModel config = new ConfigModel();
            if (!File.Exists(path))
            {
                string text = JsonSerializer.Serialize(config);
                File.WriteAllText(path, text);
            }
            string read = File.ReadAllText(path);
            config = JsonSerializer.Deserialize<ConfigModel>(read)!;
            collection.AddSingleton(config);
            collection.AddSingleton<IConfigPLC>(config);
        }
        public static void AddCommonServices(this IServiceCollection collection)
        {
            collection.AddScoped<BinService>();
            collection.AddSingleton<RunningTransactionManager>();
            collection.AddSingleton<BinInfoManager>();
            collection.AddScoped<PLCService>();
            collection.AddTransient<MainViewModel>();
            collection.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost")
);
        }
    }
}
