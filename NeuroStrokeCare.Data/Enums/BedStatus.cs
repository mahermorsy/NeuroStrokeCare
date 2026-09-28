using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.Enums
{
    public enum BedStatus
    {
        Vacant = 1,             // أخضر
        Occupied = 2,           // أزرق/أحمر
        PendingDischarge = 3,   // برتقالي
        Cleaning = 4            // أصفر
    }
}
