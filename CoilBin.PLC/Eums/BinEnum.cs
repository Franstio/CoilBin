using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoilBin.PLC.Eums
{
    public enum BinEnum
    {
        Red = 6,
        Yellow = 7,
        Green = 8,
        
        TopSensor = 0,
        BottomSensor=1,

        TopLock=4,
        BottomLock=5
    }
}
