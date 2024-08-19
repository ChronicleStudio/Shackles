using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace shackles
{
    [HarmonyPatch(typeof(BlockEntityGenericTypedContainer), "Initialize")]
    internal class PatchContainers
    {
        public static void Postfix(BlockEntityGenericTypedContainer __instance, ICoreAPI api)
        {
            if (!api.Side.IsServer())
            {
                return;
            }
            BlockPos Pos = __instance.Pos;
            ShackleGearTrackerModSystem Tracker = __instance.Api.ModLoader.GetModSystem<ShackleGearTrackerModSystem>();
            __instance.RegisterGameTickListener(delegate
            {
                __instance?.Inventory.All(delegate (ItemSlot slot)
                {
                    if (slot?.Itemstack?.Item is ItemShackle)
                    {
                        ((ItemShackle)slot.Itemstack.Item).UpdateFuelState(api.World, slot);
                        string @string = slot.Itemstack.Attributes.GetString("pearled_uid");
                        if (@string != null)
                        {
                            FullTrackData fullTrackData = Tracker?.GetTrackData(@string);
                            if (fullTrackData != null)
                            {
                                fullTrackData.SetLocation(Pos);
                                fullTrackData.SlotReference.InventoryID = slot.Inventory.InventoryID;
                                fullTrackData.SlotReference.SlotID = slot.Inventory.GetSlotId(slot);
                            }
                        }
                    }
                    return true;
                });
                Tracker?.SaveTrackToDB();
            }, 500);
        }
    }
}
