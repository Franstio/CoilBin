using CoilBin.RackApi.Models;

namespace CoilBin.RackApi.Hubs.Rack
{
    public interface IRackClient
    {
            Task weightUpdated(RackModel rack);
    }
}
