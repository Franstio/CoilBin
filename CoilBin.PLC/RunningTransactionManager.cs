using CoilBin.PLC.Models;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoilBin.PLC
{
    public class RunningTransactionManager
    {
        private const string KEY = "RunningTransactionKey";
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        public RunningTransaction RunningTransactionData { 
            get
            {

                var cur = db.GetDatabase(9);
                string? value = cur.StringGet(KEY);
                RunningTransaction tr = value is null ? new RunningTransaction() : JsonSerializer.Deserialize<RunningTransaction>(value)!;
                return tr;
            } 
        }
        private IConnectionMultiplexer db;
        public  async Task Save(RunningTransaction data)
        {
            try
            {
                await SemaphoreSlim.WaitAsync();
                var cur = db.GetDatabase(9);
                await cur.StringSetAsync(KEY, JsonSerializer.Serialize(RunningTransactionData));
            }
            catch (Exception e)
            {
                Log.Error($"From Running Transaction Manager: {e.Message}");
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        public RunningTransactionManager(IConnectionMultiplexer _db)
        {
            db = _db;
        }
        public class RunningTransaction
        {
            public RunningTransaction() { }
            public bool IsRunning { get; set; } = false;
            public string? Type { get; set; }
            public string? TopSensor { get; set; }
            public string? BottomSensor { get; set; }
            public bool IsReady { get; set; } = true;
            public bool IsVerify { get; set; } = false;
            public DateTime? StartTime { get; set; }  
            public string Message { get; set; } = string.Empty;
            public int Stage { get; set; } = 0;
            public BinModel? Bin { get; set; } = null;
        }
    }
}
