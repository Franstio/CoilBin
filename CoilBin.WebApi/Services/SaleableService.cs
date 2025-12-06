using CoilBin.PLC;
using CoilBin.PLC.Eums;
using CoilBin.PLC.Services;
using CoilBin.WebApi.Hubs.Bin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using static CoilBin.PLC.RunningTransactionManager;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CoilBin.WebApi.Services
{
    public class SaleableService
    {
        private RunningTransactionManager Manager = null!;
        private BinInfoManager BinInfoManager = null!;
        private readonly IHubContext<BinHub,IBinClient> binHubContext;
        private IServiceProvider provider;
        public SaleableService(IServiceProvider provider,IHubContext<BinHub,IBinClient> hubContext,RunningTransactionManager manager,BinInfoManager binManager ) 
        {
            this.provider = provider;
            binHubContext = hubContext;
            Manager = manager;
            BinInfoManager = binManager;

        }
        private async Task Collection(ushort[] data)
        {
            var runningData = Manager.RunningTransactionData;
            if (!runningData.IsRunning || runningData.Type != "Collection")
                return;

            if (runningData.Stage == 0 && data[(int)BinEnum.BottomSensor] == 0)
            {
                runningData.Message = "Tutup Penutup Bawah";
                runningData.BottomSensor = "1";
                runningData.Stage = 1;
                await Manager.Save(runningData);
                await binHubContext.Clients.All.target_0();
            }
            else if (runningData.Stage == 1 && data[(int)BinEnum.BottomSensor] == 1)
            {

                runningData.Message = "Tekan Tombol Lock";
                runningData.BottomSensor = "0";
                runningData.Stage = 2;
                runningData.AllowReopen = true;
                await binHubContext.Clients.All.reopen(new ReopenModel() { reopen = true,type=runningData.Type });
                await Manager.Save(runningData);
                await binHubContext.Clients.All.target_1();
                //            await BinService.EndTransaction();
            }
        }
        private async Task Dispose(ushort[] data)
        {
            var runningData = Manager.RunningTransactionData;
            if (!runningData.IsRunning || runningData.Type != "Dispose")
                return;
            if (runningData.Stage == 0 && data[(int)BinEnum.TopSensor] == 0)
            {
                runningData.Message = "Tutup Penutup Atas";
                runningData.TopSensor = "1";
                runningData.Stage = 1;
                await binHubContext.Clients.All.target_top_0();
            }
            else if (runningData.Stage == 1 && data[(int)BinEnum.TopSensor] == 1)
            {
                runningData.Message = "Lakukan Verifikasi";
                runningData.TopSensor = "0";
                runningData.IsRunning = false;
                runningData.IsVerify = true;
                runningData.Stage = 2;
                await binHubContext.Clients.All.target_top_1();
            }
            await Manager.Save(runningData);
        }
        public async Task TransactionLoop()
        {
            while (true)
            {

                using (var scope = provider.CreateScope())
                {

                    var BinService = scope.ServiceProvider.GetRequiredService<BinService>();
                    var runningTransaction = Manager.RunningTransactionData;
                    var binInfo = BinInfoManager.BinInfoModel;

                    await binHubContext.Clients.All.UpdateInstruksi(runningTransaction.Message);
                    if ((runningTransaction.IsRunning && runningTransaction.StartTime is not null && runningTransaction.StartTime.Value.AddSeconds(30) <= DateTime.Now) || runningTransaction.AllowReopen)
                        await binHubContext.Clients.All.reopen(new ReopenModel() { reopen = true,type = runningTransaction.Type });
                    else
                        await binHubContext.Clients.All.reopen(new ReopenModel() { reopen = false,type=runningTransaction.Type });
                    await binHubContext.Clients.All.GetType(runningTransaction.Type);
                    await Task.Delay(300);
                }
            }
        }
        public async Task SensorLoop()
        {
            while (true)
            {
                try
                {
                    using (var scope = provider.CreateScope())
                    {

                        var BinService = scope.ServiceProvider.GetRequiredService<BinService>();
                        var runningTransaction = Manager.RunningTransactionData;
                        var binInfo = BinInfoManager.BinInfoModel;
                        decimal limit = (binInfo.Max_Weight / 100) * 90;
                        bool overLimit = binInfo.Weight >= binInfo.Max_Weight;
                        //await BinService.SwitchBinFeature(BinEnum.Yellow, !runningTransaction.IsRunning && !overLimit);
                        await BinService.SwitchBinFeature(BinEnum.Red, binInfo.Weight > limit);
                        var data = await BinService.ReadingSensor();
                        await binHubContext.Clients.All.sensorUpdate(
                            [data[(int)BinEnum.TopSensor], data[(int)BinEnum.BottomSensor], data[(int)BinEnum.Red], data[(int)BinEnum.Yellow], data[(int)BinEnum.Green],data[(int)BinEnum.TopLock],data[(int)BinEnum.BottomLock] ]);
                        await binHubContext.Clients.All.Bin(binInfo);
                        if (runningTransaction.IsReady)
                        {
                            if (data[(int)BinEnum.Yellow] == 0)
                                await BinService.SwitchBinFeature(BinEnum.Yellow, true);
                            if (data[(int)BinEnum.Green] == 1)
                                await BinService.SwitchBinFeature(BinEnum.Green, false);
                        }
                        else
                        {
                            if (data[(int)BinEnum.Yellow] == 1)
                                await BinService.SwitchBinFeature(BinEnum.Yellow, false);
                            if (data[(int)BinEnum.Green] == 0)
                                await BinService.SwitchBinFeature(BinEnum.Green, true);
                        }
                        if (runningTransaction.IsRunning && runningTransaction.Type == "Collection")
                            await Collection(data);
                        else
                            await Dispose(data);
                    }
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    Log.Error($"From SensorLoop: {ex.Message} | {ex.StackTrace}");
                }
            }
        }
    }
}
