using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shackles
{
    internal class SlotReference
    {
        public int SlotID { get; set; }

        public string InventoryID { get; set; }

        public SlotReference(int slotID, string inventoryID)
        {
            SlotID = slotID;
            InventoryID = inventoryID;
        }
    }
}
