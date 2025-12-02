using CoilBin.PLC.Services;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.PLC.Extension
{
    public static class CommonServiceExtension
    {
        public static void AddCommonServices(this IServiceCollection collection)
        {
            collection.AddScoped<BinService>();
            collection.AddSingleton<RunningTransactionManager>();
            collection.AddSingleton<BinInfoManager>();
            collection.AddScoped<PLCService>();
            collection.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost")
);
        }
    }
}
