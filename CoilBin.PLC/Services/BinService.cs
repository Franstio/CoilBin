using CoilBin.PLC;
using CoilBin.PLC.Eums;
using CoilBin.PLC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.PLC.Services
{
    public class BinService
    {
        
        private readonly PLCService plcService;
        private readonly RunningTransactionManager manager;
        public bool PLCStatus => PLCService.PLCStatus;

        public BinService(PLCService pLCService,RunningTransactionManager _manager)
        {
            plcService = pLCService;
            manager = _manager;
        }

        public async Task<ushort[]> ReadingSensor()
        {
            return await plcService.ReadData(0, 10);
        }

        public async Task SwitchBinFeature(BinEnum lamp,bool enable)
        {
            await plcService.WriteData((byte)lamp, (byte)(enable ? 1 : 0));
        }
        
        public async Task StartTransaction(BinModel bin)
        {
//            await SwitchBinFeature(BinEnum.Yellow, false);
//            await SwitchBinFeature(BinEnum.Green, true);
            var trData = manager.RunningTransactionData;
            bool isCollection = bin.Type == "Collection";
            trData.Message = isCollection ? "Buka Penutup Bawah" : "Buka Penutup Atas";
            BinEnum _lock = isCollection ? BinEnum.BottomLock : BinEnum.TopLock;
            await SwitchBinFeature(_lock, true);
            trData.TopSensor = isCollection ? null : "0";
            trData.BottomSensor = isCollection ? "1" : null;
            trData.IsRunning = true;
            trData.IsReady = false;
            trData.IsVerify = false;
            trData.AllowReopen = false;
            trData.Type = isCollection ? "Collection" : "Dispose";
            trData.StartTime = DateTime.Now;
            trData.Stage = 0;
            await manager.Save(trData);

        }
        public async Task ClearBin()
        {
 //           await SwitchBinFeature(BinEnum.Yellow, true);
 //           await SwitchBinFeature(BinEnum.Green, false);
            RunningTransactionManager.RunningTransaction tr = new();
            await manager.Save(tr);
        }
        public async Task EndTransaction()
        {
//            await SwitchBinFeature(BinEnum.Yellow, true);
//            await SwitchBinFeature(BinEnum.Green, false);
            var trData = manager.RunningTransactionData;
            if (trData.Type == "Dispose")
            {
                trData.Message = "Data Telah Masuk";
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    var dt = manager.RunningTransactionData;
                    dt.Message = "";
                    await manager.Save(dt);
                });
            }
            else if (trData.Type == "Collection")
                await SwitchBinFeature(BinEnum.BottomLock, true);
            trData.IsRunning = false;
            trData.TopSensor = null;
            trData.BottomSensor = null;
            trData.IsReady = true;
            trData.Message = "";
            trData.IsVerify = false;
            trData.Type = null;
            trData.StartTime = null;
            trData.Stage = 0;
            trData.AllowReopen = false;
            await manager.Save(trData);
        }

        public string GetBinIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).FirstOrDefault()?.ToString() ?? "-";
        }

    }
}
