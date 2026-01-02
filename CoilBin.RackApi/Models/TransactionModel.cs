namespace CoilBin.RackApi.Models
{
    public class TransactionModel
    {
        public int id { get; set; }

        public int badgeId { get; set; }
        public int idContainer { get; set; }
        public int idWaste { get; set; }
        public string type { get; set; } = string.Empty;
        public decimal weight { get; set; } = 0;
        public DateTime recordDate { get; set; } = DateTime.Now;

        public string idqrmachine { get; set; } = string.Empty;
    }
}
