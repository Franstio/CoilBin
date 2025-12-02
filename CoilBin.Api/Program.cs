using CoilBin.PLC;
using CoilBin.PLC.Contracts;
using CoilBin.PLC.Eums;
using CoilBin.PLC.Models;
using CoilBin.PLC.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SocketIOClient;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilBin.Api
{
    public class Program
    {
        public static SocketIOClient.SocketIO ClientIO { get; private set; } = null!;
        public static WebApplicationBuilder ConfigWeb(string[] args)
        {
            var builder = WebApplication.CreateSlimBuilder(args);

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
            });

            foreach (var service in builder.Services)
                ServicesLocator.ServiceCollection.Add(service);
            ServicesLocator.ServiceCollection.AddSingleton(builder.Configuration);
            ServicesLocator.ServiceCollection.AddSingleton<IConfiguration>(builder.Configuration);
            return builder;
        }
        public static WebApplication BuildWeb(WebApplicationBuilder builder)
        {

            

            var app = builder.Build();

            app.MapPost("/Start", async (Dictionary<string,BinModel> bin) =>
            {
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
            app.MapGet("/Status", ()=>{
                RunningTransactionManager manager = app.Services.GetRequiredService<RunningTransactionManager>();
                var tr = manager.RunningTransactionData;
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
            ClientIO = new SocketIOClient.SocketIO(app.Configuration.GetSection("Timbangan").Value!);

            ClientIO.On("getweight", async (res) =>
            {
                BinModel bin = res.GetValue<BinModel>();
                RunningTransactionManager manager = app.Services.GetRequiredService<RunningTransactionManager>();
                BinInfoManager binManager = app.Services.GetRequiredService<BinInfoManager>();

                await binManager.Save(bin);

                var binService = app.Services.GetRequiredService<BinService>();
                var tr = manager.RunningTransactionData;

                if (!tr.IsRunning && tr.IsReady && bin.Dispose )
                {
                    bin.Type = "Dispose";

                    await binService.StartTransaction(bin);
                }
                else if (!tr.IsRunning && !tr.IsReady && tr.IsVerify && tr.Type == "Dispose" && (!bin.Dispose))
                {
                    await binService.EndTransaction();
                }
            });

            return app;
        }
        public static void Main(string[] args)
        {
            var config = ConfigWeb(args);
            var app = BuildWeb(config);

            app.Run();
        }
    }

    public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

    [JsonSerializable(typeof(Todo[]))]
    internal partial class AppJsonSerializerContext : JsonSerializerContext
    {

    }
}
