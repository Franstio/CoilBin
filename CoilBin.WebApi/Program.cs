using CoilBin.PLC;
using CoilBin.PLC.Contracts;
using CoilBin.PLC.Eums;
using CoilBin.PLC.Extension;
using CoilBin.PLC.Models;
using CoilBin.PLC.Services;
using CoilBin.WebApi.Hubs.Bin;
using CoilBin.WebApi.Models;
using CoilBin.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using System.Diagnostics;
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
                                      policy.SetIsOriginAllowed(x => true);
                                      policy.AllowCredentials();
                                      policy.AllowAnyHeader();
                                      policy.AllowAnyMethod();
                                  });
            });
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });
            return builder;
        }
        static void BuildService(WebApplicationBuilder builder)
        {
            
        }
        public static Task<WebApplication> BuildWeb(WebApplicationBuilder builder)
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
            app.MapPost("/End", async (BinService binService) =>
            {
                await binService.EndTransaction();
                return Results.Ok();
            });
            app.MapGet("/Status", (RunningTransactionManager manager) => {
                var tr = manager.RunningTransactionData;
                return Results.Ok(tr);
            });
            app.MapGet("/Status-Bin", (BinInfoManager manager) => {
                var tr = manager.BinInfoModel;
                return Results.Ok(tr);
            });
            app.MapGet("/Clear-Bin", async (IHubContext<BinHub, IBinClient> hubContext, BinService binService) =>
            {

                await binService.ClearBin();
                await hubContext.Clients.All.reload(new ReloadModel() { reload = true });
                return Results.Json(new {msg="ok"});
            });
            app.MapGet("/Feature", async (BinService binService,[FromQuery] string type, [FromQuery] string value) =>
            {
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
            app.MapPost("/observeTopSensor", (object readTargetTop) =>
            {
                return Results.Ok();
            });
            app.MapPost("/observeBototmSensor", (object readTarget) =>
            {
                return Results.Ok();
            });
            app.MapPost("/lockBottom", async (BinService binService, RunningTransactionManager manager,ReopenPayloadModel.BottomLock payload) =>
            {
                var rt = manager.RunningTransactionData;
                rt.StartTime = null;
                rt.AllowReopen = false;
                await manager.Save(rt);
                await binService.SwitchBinFeature(BinEnum.BottomLock, true);
                return Results.Ok();
            });
            app.MapPost("/lockTop", async (BinService binService, RunningTransactionManager manager,ReopenPayloadModel.TopLock payload) =>
            {
                var rt = manager.RunningTransactionData;
                rt.StartTime = null;
                rt.AllowReopen = false;
                await manager.Save(rt);
                await binService.SwitchBinFeature(BinEnum.TopLock, true);
                return Results.Ok();
            });
            app.MapGet("/hostname", () =>
            {
                string hostname = System.Net.Dns.GetHostName();
                return Results.Json(new { hostname });
            });
            app.MapGet("/ip",  (BinService binService) =>
            {
                string ip = binService.GetBinIpAddress();
                return Results.Json(new object[] {ip});
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
        public class WebConfigPLC : IConfigPLC
        {
            public string USBPATH { get; set; } = string.Empty;
        }

        static WebApplicationBuilder AddBaseService(WebApplicationBuilder builder)
        {
            ServicesLocator.ServiceCollection.AddCommonServices();
            foreach (var service in ServicesLocator.ServiceCollection)
                builder.Services.Add(service);
            WebConfigPLC plcConfig = new WebConfigPLC();
            plcConfig.USBPATH = builder.Configuration.GetSection("USBPATH").Value!;
            builder.Services.AddSingleton<IConfigPLC>(plcConfig);
            builder.Services.AddSingleton<SaleableService>();
            builder.Services.AddSignalR();
            return builder;
        }
        public static void Main(string[] args)
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
            var configWeb = ConfigWeb(args);
            configWeb = AddBaseService(configWeb);
            var app = BuildWeb(configWeb).Result;
            app.MapHub<BinHub>("/hub/bin");

            var saleable = app.Services.GetRequiredService<SaleableService>();
            _ = Task.Run(saleable.SensorLoop);
            _ = Task.Run(saleable.TransactionLoop);
            app.Run("http://*:5000");
        }
    }
}
