using CoilBin.PLC.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.Models
{
    public class ConfigModel : IConfigPLC
    {
        public string USBPATH { get; set; } = string.Empty;

        public string Timbangan { get; set; } = string.Empty;
    }
}
