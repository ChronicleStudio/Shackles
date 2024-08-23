using Shackles.Config;
using Shackles.ShackleSystems;
using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Shackles.Items
{
	public class ItemShackle : Item
	{
		private ICoreServerAPI sapi;

		private ICoreClientAPI capi;

		private double fuelMult;

		private double maxSeconds;

		public PrisonController Prsn => api.ModLoader.GetModSystem<ShacklesModSystem>().Prison;

		public ShackleTrackerModSystem Tracker => api.ModLoader.GetModSystem<ShackleTrackerModSystem>();

		internal ShacklesModServerConfig Config => api.ModLoader.GetModSystem<ShacklesModSystem>().config;

		public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);
			sapi = api as ICoreServerAPI;
			capi = api as ICoreClientAPI;
			fuelMult = Attributes["fuelmult"].AsDouble(1.0);
			maxSeconds = Attributes["maxseconds"].AsDouble(1210000.0);
		}

		public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack = null)
		{
			base.OnModifiedInInventorySlot(world, slot, extractedStack);
		}

		public override void OnGroundIdle(EntityItem entityItem)
		{
			base.OnGroundIdle(entityItem);
			ITreeAttribute treeAttribute = entityItem.Slot?.Itemstack?.Attributes;
			if (treeAttribute != null && treeAttribute.GetString("pearled_uid") != null && entityItem.Collided)
			{
				Prsn?.FreePlayer(treeAttribute.GetString("pearled_uid"), entityItem.Slot, destroy: true, ((Entity)entityItem).Pos.AsBlockPos.UpCopy());
				entityItem.Die();
			}
		}

		public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
		{
			handling = EnumHandHandling.PreventDefault;
			ITreeAttribute treeAttribute = slot?.Itemstack?.Attributes;
			BlockPos blockPos = blockSel?.Position;
			if (sapi == null || treeAttribute == null)
			{
				return;
			}
			Block obj = ((blockPos != null) ? sapi.World.BlockAccessor.GetBlock(blockPos) : null);
			string @string = treeAttribute.GetString("pearled_uid");
			double @double = treeAttribute.GetDouble("shackled_cell_cooldown");
			if (obj is BlockBed)
			{
				if (!(api.World.Calendar.TotalHours > @double) || @string == null)
				{
					return;
				}
				{
					foreach (ItemSlot item in slot.Inventory)
					{
						ItemStack itemStack = item?.Itemstack;
						if (itemStack?.Item is ItemRustyGear && itemStack.StackSize >= 2)
						{
							item.TakeOut(2);
							item.MarkDirty();
							slot.MarkDirty();
							treeAttribute.SetDouble("shackled_cell_cooldown", api.World.Calendar.TotalHours + 24.0);
							treeAttribute.SetBlockPos("shackled_cell", blockPos);
							Prsn.SetCellSpawn(@string, blockPos);
							Prsn.MoveToCell(@string);
							break;
						}
					}
					return;
				}
			}
			if (byEntity.Controls.Sneak && @string != null)
			{
				Prsn.FreePlayer(@string, slot, destroy: true, blockPos.UpCopy());
				return;
			}
			double double2 = treeAttribute.GetDouble("pearled_fuel");
			foreach (ItemSlot item2 in slot.Inventory)
			{
				if (double2 > maxSeconds)
				{
					break;
				}
				CollectibleObject collectibleObject = item2?.Itemstack?.Collectible;
				if (collectibleObject?.CombustibleProps != null && collectibleObject.CombustibleProps.BurnTemperature >= 1000)
				{
					double num = (double)collectibleObject.CombustibleProps.BurnTemperature / 1000.0 * (double)collectibleObject.CombustibleProps.BurnDuration;
					num *= fuelMult;
					num *= (double)Config.shackleBurnTimeMul;
					treeAttribute.SetDouble("pearled_fuel", double2 + num);
					item2.TakeOut(1);
					item2.MarkDirty();
					slot.MarkDirty();
					if (treeAttribute.GetString("pearled_uid") != null)
					{
						Tracker.SetLastFuelerUID(treeAttribute.GetString("pearled_uid"), (byEntity as EntityPlayer).PlayerUID);
					}
					break;
				}
			}
		}

		public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
		{
			ITreeAttribute obj = inSlot?.Itemstack?.Attributes;
			string text = obj?.GetString("pearled_name");
			string text2 = obj?.GetString("pearled_uid");
			double value = Math.Round(obj?.GetDouble("pearled_fuel") ?? 0.0, 3);
			double num = obj?.GetDouble("shackled_cell_cooldown") ?? 0.0;
			bool flag = num > world.Calendar.TotalHours;
			int num2 = (int)Math.Round(num - world.Calendar.TotalHours);
			TimeSpan timeSpan = TimeSpan.FromSeconds(value);
			dsc.AppendLine("Shackled: " + text).AppendLine("UID: " + text2).AppendLine("Remaining Time: " + timeSpan.ToString("dd\\:hh\\:mm\\:ss"));
			dsc.AppendLine(string.Format("Can {0} set cell spawn {1}", (text == null || flag) ? "not" : "", (text == null) ? "nobody imprisoned!" : (flag ? $"now, must wait {num2} game hours before next cell set." : "now.")));
			base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		}

		public void UpdateFuelState(IWorldAccessor world, ItemSlot inSlot)
		{
			if (inSlot == null || !world.Side.IsServer() || inSlot is ItemSlotCreative)
			{
				return;
			}
			ITreeAttribute treeAttribute = inSlot?.Itemstack?.Attributes;
			if (treeAttribute != null && treeAttribute.GetString("pearled_uid") != null)
			{
				long ticks = DateTime.UtcNow.Ticks - treeAttribute.GetLong("pearled_lastping", DateTime.UtcNow.Ticks);
				double @double = treeAttribute.GetDouble("pearled_fuel");
				if (@double < 0.0)
				{
					Prsn.FreePlayer(treeAttribute.GetString("pearled_uid"), inSlot);
				}
				else
				{
					treeAttribute.SetDouble("pearled_fuel", @double - new TimeSpan(ticks).TotalSeconds);
				}
				treeAttribute.SetLong("pearled_lastping", DateTime.UtcNow.Ticks);
			}
			inSlot.MarkDirty();
		}
	}
}
