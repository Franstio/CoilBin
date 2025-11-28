using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.PLC.Models
{
    public class BinModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public decimal? Weight { get; set; }

        public int? IdWaste { get; set; }

        public decimal? Max_Weight { get; set; }

        public string? Name_Hostname { get; set; }

        public bool? Dispose { get; set; }

        public bool? Disabled { get; set; }
        public string? Type { get; set; } 
    }
}
