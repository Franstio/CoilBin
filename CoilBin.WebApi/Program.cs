using CoilBin.PLC;
using CoilBin.PLC.Eums;
using CoilBin.PLC.Models;
using CoilBin.PLC.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoilBin.WebApi
{
    public class Program
    {
        public static SocketIOClient.SocketIO ClientIO { get; private set; } = null!;
        private static Task BroadcastTask = Task.CompletedTask;
        public static WebApplicationBuilder ConfigWeb(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHttpClient().ConfigureHttpClientDefaults(act =>
            {
            }) ;

            //foreach (var service in builder.Services)
            //    ServicesLocator.ServiceCollection.Add(service);
            //ServicesLocator.ServiceCollection.AddSingleton(builder.Configuration);
            //ServicesLocator.ServiceCollection.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: "allowAll",
                                  policy =>
                                  {
                                      policy.AllowAnyHeader();
                                      policy.AllowAnyMethod();
                                      policy.AllowAnyOrigin();
                                  });
            });
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });
            return builder;
        }
        public static  Task<WebApplication> BuildWeb(WebApplicationBuilder builder)
        {



            var app = builder.Build();
            app.UseCors("allowAll");
            app.MapPost("/Start", async (Dictionary<string,BinModel> bin) =>
            {
                Log.Information(JsonSerializer.Serialize(bin));
                var binService = app.Services.GetRequiredService<BinService>();
                await binService.StartTransaction(bin["bin"]);
                return Results.Ok();
            });
            app.MapGet("/End", async () =>
            {

                var binService = app.Services.GetRequiredService<BinService>();
                await binService.EndTransaction();
                return Results.Ok();
            });
            app.MapGet("/Status", () => {
                RunningTransactionManager manager = app.Services.GetRequiredService<RunningTransactionManager>();
                var tr = manager.RunningTransactionData;
                return Results.Ok(tr);
            });
            app.MapGet("/Status-Bin", () => {
                BinInfoManager manager = app.Services.GetRequiredService<BinInfoManager>();
                var tr = manager.BinInfoModel;
                return Results.Ok(tr);
            });
            app.MapGet("/Clear-Bin", async () =>
            {

                var binService = app.Services.GetRequiredService<BinService>();
                await binService.ClearBin();
                return Results.Ok();
            });
            app.MapGet("/Feature", async ([FromQuery] string type, [FromQuery] string value) =>
            {
                var binService = app.Services.GetRequiredService<BinService>();
                switch (type)
                {
                    case "Red":
                        await binService.SwitchBinFeature(BinEnum.Red, value == "1");
                        break;
                    case "Yellow":
                        await binService.SwitchBinFeature(BinEnum.Yellow, value == "1"); break;
                    case "Green":
                        await binService.SwitchBinFeature(BinEnum.Green, value == "1"); break;
                    case "Top":
                        await binService.SwitchBinFeature(BinEnum.TopLock, value == "1"); break;
                    case "Bottom":
                        await binService.SwitchBinFeature(BinEnum.BottomLock, value == "1"); break;
                }
                return Results.Ok();
            });

            BroadcastTask = Task.Run( async () =>
            {
                while (true)
                {
                    var clientFactory = app.Services.GetRequiredService<IHttpClientFactory>();
                    var client = clientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(1);
                    string text = "";
                    try
                    {
                        string hostname = System.Net.Dns.GetHostName();
                        client.BaseAddress = new Uri(app.Configuration.GetSection("Timbangan").Value!);
                        var res = await client.GetAsync($"getBinData?hostname={hostname}");
                         text = await res.Content.ReadAsStringAsync();
                        Dictionary<string, BinModel> data = JsonSerializer.Deserialize<Dictionary<string, BinModel>>(text, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true,
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                        })!;
                        var bin = data["bin"];

                        RunningTransactionManager manager = app.Services.GetRequiredService<RunningTransactionManager>();
                        BinInfoManager binManager = app.Services.GetRequiredService<BinInfoManager>();
                        await binManager.Save(bin);

                        var binService = app.Services.GetRequiredService<BinService>();
                        var tr = manager.RunningTransactionData;

                        if (!tr.IsRunning && tr.IsReady && bin.Dispose)
                        {
                            bin.Type = "Dispose";

                            await binService.StartTransaction(bin);
                        }
                        else if (!tr.IsRunning && !tr.IsReady && tr.IsVerify && tr.Type == "Dispose" && (!bin.Dispose))
                        {
                            await binService.EndTransaction();
                        }
                    }
                    catch(Exception e)
                    {
                        Log.Error($"From broadcast task {client.BaseAddress} {e.Message} {e.StackTrace}");
                        Log.Error(text);
                    }
                    await Task.Delay(300);
                }
            });
            //ClientIO = new SocketIOClient.SocketIO(app.Configuration.GetSection("Timbangan").Value!);

            //ClientIO.On("getweight", async (res) =>
            //{
            //    try
            //    {
            //        var obj = res.GetValue<object>();
            //        Log.Information("Before serialization: " +JsonSerializer.Serialize(obj));
            //        BinModel bin = JsonSerializer.Deserialize<BinModel>(JsonSerializer.Serialize(obj),new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
            //        Log.Information(JsonSerializer.Serialize(bin));

            //        RunningTransactionManager manager = app.Services.GetRequiredService<RunningTransactionManager>();
            //        BinInfoManager binManager = app.Services.GetRequiredService<BinInfoManager>();
            //        await binManager.Save(bin);

            //        var binService = app.Services.GetRequiredService<BinService>();
            //        var tr = manager.RunningTransactionData;

            //        if (!tr.IsRunning && tr.IsReady && bin.Dispose.HasValue && bin.Dispose.Value)
            //        {
            //            bin.Type = "Dispose";

            //            await binService.StartTransaction(bin);
            //        }
            //        else if (!tr.IsRunning && !tr.IsReady && tr.IsVerify && tr.Type == "Dispose" && (!bin.Dispose.HasValue || !bin.Dispose.Value))
            //        {
            //            await binService.EndTransaction();
            //        }
            //    }
            //    catch(Exception e)
            //    {
            //        Log.Error($"From getweight listener: {e.Message}");
            //    }
            //});
            //ClientIO.OnConnected +=  (s,e) =>
            //{
            //    if (BroadcastTask.IsCompleted)
            //    {
            //        BroadcastTask = Task.Run(async () =>
            //        {
            //            while (true)
            //            {
            //                try
            //                {
            //                    await Task.Delay(1000);
            //                    if (!ClientIO.Connected)
            //                        continue;
            //                    string hostname = System.Net.Dns.GetHostName();
            //                    Log.Information($"Broadcast data to timbangan");
            //                    await ClientIO.EmitAsync("getWeightBin", hostname);
            //                }
            //                catch (Exception e) {
            //                    Log.Error($"Error broadcast data: {e.Message}");
            //                }
            //            }
            //        });
            //    }
            //};
            //ClientIO.OnReconnectFailed += (s, e) =>
            //{
            //    Log.Error($"Failed to connect {app.Configuration.GetSection("Timbangan").Value!}, reconnecting ");
            //};
            //ClientIO.OnReconnectError += (s, e) =>
            //{
            //    Log.Error($"Error when connecting {app.Configuration.GetSection("Timbangan").Value!}, {e.Message}");
            //};
            //await ClientIO.ConnectAsync();
            return Task.FromResult(app);
        }
        public static void Main(string[] args)
        {
            var configWeb = ConfigWeb(args);

            var app = BuildWeb(configWeb);
            app.Result.Run("http://*:5000");
        }
    }
}
