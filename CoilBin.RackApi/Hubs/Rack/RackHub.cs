using CoilBin.RackApi.Hubs.Rack;
using CoilBin.RackApi.Models;
using Microsoft.AspNetCore.SignalR;

namespace CoilBin.RackApi.Hubs.Rack
{

    public class RackHub : Hub<IRackClient>
    {

    }
}
