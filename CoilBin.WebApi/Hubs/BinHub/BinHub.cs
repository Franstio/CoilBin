using CoilBin.PLC.Models;
using Microsoft.AspNetCore.SignalR;

namespace CoilBin.WebApi.Hubs.Bin
{
    public class BinHub : Hub<IBinClient>
    {
        public async Task TriggerWeight(BinModel bin)
        {

        }
    }
}
