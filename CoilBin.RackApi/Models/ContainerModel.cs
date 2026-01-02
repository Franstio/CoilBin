namespace CoilBin.RackApi.Models
{
    public class ContainerModel
    {
        public int containerId { get; set; }    

        public string name { get; set; } = null!;
        public string IdWaste { get; set; } = null!;
        public string station { get; set; } = null!;
        public decimal weightbin { get; set; } = 0;
        public string status { get; set; } = null!;
        public int line { get; set; } 
        public string type { get; set; } = null!;
    }
}
