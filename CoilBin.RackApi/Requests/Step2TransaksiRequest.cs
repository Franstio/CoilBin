using CoilBin.RackApi.Models;

namespace CoilBin.RackApi.Requests
{
    public class Step2TransaksiRequest
    {
        public string name { get; set; } = null!;

        public string waste { get; set; } = null!;  
        public string containerName { get; set; } = null!;
        public TransactionModel payload { get; set; } = null!;
    }
}
