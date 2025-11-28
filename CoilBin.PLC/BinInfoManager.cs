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
    public class BinInfoManager
    {
        private const string KEY = "BinInfoKey";
        private static SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        public BinModel BinInfoModel { 
            get
            {

                var cur = db.GetDatabase(9);
                string? value = cur.StringGet(KEY);
                BinModel tr = value is null ? new BinModel() : JsonSerializer.Deserialize<BinModel>(value)!;
                return tr;
            } 
        }
        private IConnectionMultiplexer db;
        public  async Task Save(BinModel data)
        {
            try
            {
                await SemaphoreSlim.WaitAsync();
                var cur = db.GetDatabase(7);
                await cur.StringSetAsync(KEY, JsonSerializer.Serialize(BinInfoModel));
            }
            catch (Exception e)
            {
                Log.Error($"From Bin Info Manager: {e.Message}");
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }
        public BinInfoManager(IConnectionMultiplexer _db)
        {
            db = _db;
        }
    }
}
