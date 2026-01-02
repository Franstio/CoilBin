namespace CoilBin.RackApi.Models
{
    public class RackModel
    {
        public string name { get; set; } = null!;
        public int clientId { get; set; } 
        public decimal weight { get; set; } = 0;
        public decimal weightbin { get; set; } = 0;
        public int wasteId { get; set; } 
        public int rackId { get; set; }
        public int address { get; set; }
        public int value { get; set; }
        public int line { get; set; }
        public decimal max_weight { get; set; }
        public int sensor { get; set; }
    }
}
