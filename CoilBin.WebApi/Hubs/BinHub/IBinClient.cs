using CoilBin.PLC.Models;

namespace CoilBin.WebApi.Hubs.Bin
{
    public interface IBinClient
    {
        Task UpdateInstruksi(string instruksi);

        Task sensorUpdate(ushort[] data);
        Task target_top_0();
        Task target_top_1();
        Task target_1();
        Task target_0();
        
        Task reopen(ReopenModel reopen);

        Task GetType(string? type);

        Task Bin(BinModel bin);
        Task reload(ReloadModel reload);

    }
}
