using CoilBin.PLC;
using CoilBin.RackApi.Models;

namespace CoilBin.RackApi.Services
{
    public class RackPLCService
    {
        private readonly PLCService plcService;

        public RackPLCService(PLCService plcService)
        {
            this.plcService = plcService;
        }

        public async Task<bool> TriggerRack(RackModel rack, bool enable)
        {
            
            return await plcService.WriteData((byte)rack.address,(byte)( enable ? 1 : 0));
        }
    }
}
