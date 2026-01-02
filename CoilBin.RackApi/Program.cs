
using CoilBin.PLC.Contracts;
using CoilBin.PLC.Extension;
using CoilBin.RackApi.Factory;
using CoilBin.RackApi.Hubs.Rack;
using CoilBin.RackApi.Services;
using MySql.Data.MySqlClient;
using System.Data;

namespace CoilBin.RackApi
{
    public class WebConfigPLC : IConfigPLC
    {
        public string USBPATH { get; set; } = string.Empty;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
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
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddPLCService();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<RackPLCService>();
            builder.Services.AddScoped<RackSaleableServices>();
            builder.Services.AddSingleton<IDBFactory<MySqlConnection>, MysqlConnectionFactory>();
            var plcConfig = new WebConfigPLC();
            plcConfig.USBPATH = builder.Configuration.GetSection("USBPATH").ToString()!;
            builder.Services.AddSingleton<IConfigPLC>(plcConfig);

            

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            var app = builder.Build();

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var scope = app.Services.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<RackSaleableServices>();
                    try
                    {
                        await Task.Delay(5000);
                        await service.SyncEmployeeTimbangan();
                    }
                    catch { }
                }
            });
            app.UseCors("allowAll");
            app.MapGet("/ping", () => Results.Ok(new {msg="ok"}));
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.MapHub<RackHub>("/hub/rack");
            app.UseAuthorization();


            app.MapControllers();

            app.Run("http://*:5001");

        }
    }
}
