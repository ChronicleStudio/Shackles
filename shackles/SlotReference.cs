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
