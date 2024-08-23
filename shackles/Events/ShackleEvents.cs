using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Shackles.Items;
using Shackles.ShackleSystems;
using Shackles.ReflectionUtil;
using System.Collections.Generic;
using Shackles.Data;
using System.Diagnostics;

namespace Shackles.Events
{
	public static class ShackleEvents
	{		

		public static void OnEntityDeath(Entity entity, DamageSource damageSource)
		{
			ICoreAPI api = entity.Api;
			ShacklesModSystem shacklesMod = api.ModLoader.GetModSystem<ShacklesModSystem>();

			if (entity is EntityPlayer entityPlayer)
			{
				OnPlayerDeath((IServerPlayer)entityPlayer.Player, damageSource);
			}
			if (shacklesMod.dummyPlayerType != null && entity.GetType() == shacklesMod.dummyPlayerType)
			{
				IServerPlayer byplayer = (IServerPlayer)api.World.PlayerByUid(entity.GetField<string>("sourceEntityUID"));
				OnPlayerDeath(byplayer, damageSource);
			}
		}

		public static void OnPlayerDeath(IServerPlayer byplayer, DamageSource damagesource)
		{
			ICoreAPI api = byplayer.Entity.Api;
			PrisonController Prison = api.ModLoader.GetModSystem<ShacklesModSystem>().Prison;

			if (!(damagesource?.SourceEntity is EntityPlayer))
			{
				return;
			}
			IPlayer killer = api.World.PlayerByUid(((EntityPlayer)damagesource.SourceEntity).PlayerUID);
			killer.Entity.WalkInventory(delegate (ItemSlot slot)
			{
				if (slot?.Itemstack?.Item is ItemShackle && (slot == null || slot.Itemstack.Attributes.GetString("pearled_uid") == null))
				{
					Prison.TryImprisonPlayer(byplayer, (IServerPlayer)killer, slot);
					return false;
				}
				return true;
			});
		}

		public static void EventOnPlayerDisconnect(IServerPlayer byplayer)
		{
			ICoreAPI api = byplayer.Entity.Api;
			ShacklesModSystem shacklesMod = api.ModLoader.GetModSystem<ShacklesModSystem>();


			if (shacklesMod.TrackerIDs.ContainsKey(byplayer.PlayerUID))
			{
				api.Event.UnregisterGameTickListener(shacklesMod.TrackerIDs[byplayer.PlayerUID]);
			}
			foreach (KeyValuePair<string, IInventory> inventory in byplayer.InventoryManager.Inventories)
			{
				string className = inventory.Value.ClassName;
				if (className == "chest" || className == "creative" || className == "ground" || className == "creative")
				{
					continue;
				}
				foreach (ItemSlot item in inventory.Value)
				{
					if (!(item is ItemSlotCreative) && item?.Itemstack?.Item is ItemShackle && item.Itemstack.Attributes.GetString("pearled_uid") != null)
					{
						ItemStack itemstack = item.TakeOutWhole();
						api.World.SpawnItemEntity(itemstack, byplayer.Entity.ServerPos.XYZ);
					}
				}
			}
		}

		public static void RegisterPearlUpdate(IServerPlayer player)
		{
			ICoreServerAPI api = player.Entity.Api as ICoreServerAPI;
			ShacklesModSystem shacklesMod = api.ModLoader.GetModSystem<ShacklesModSystem>();

			FullTrackData data = shacklesMod.Tracker.GetTrackData(player.PlayerUID);
			if (!(data?.LastPos != null) || player == null || player.PlayerUID == null)
			{
				return;
			}
			if (player.Role.Code != shacklesMod.config.shackledGroup && !shacklesMod.oldRoles.ContainsKey(player.PlayerUID))
			{
				shacklesMod.oldRoles.Add(player.PlayerUID, player.Role.Code);

			}

			api.Permissions.SetRole(player, shacklesMod.config.shackledGroup);


			string uid = player.PlayerUID;
			shacklesMod.TrackerIDs[uid] = api.Event.RegisterGameTickListener(delegate
			{
				try
				{
					data.LoadMyChunk();
					if (data.IsChunkForceLoaded && data.ItemStack?.Item is ItemShackle)
					{
						((ItemShackle)data.ItemStack.Item).UpdateFuelState(api.World, data.Slot);
					}
				}
				catch (Exception)
				{
					data.MarkUnloadable();
					api.Event.UnregisterGameTickListener(shacklesMod.TrackerIDs[uid]);
				}
			}, 500);
			shacklesMod.SaveRoleDataToDB();
		}
	}
}
