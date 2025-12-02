using CoilBin.Models;
using CoilBin.PLC.Contracts;
using CoilBin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoilBin.Extensions
{
    public static class DesktopServiceExtensions
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
            collection.AddTransient<MainViewModel>();
        }
    }
}
