using Shackles.Data;
using Shackles.Items;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Shackles.Commands
{
	public static class ShackleCommands
	{
		public static TextCommandResult FuelLog(TextCommandCallingArgs args)
		{
			var player = args.Caller.Player;
			var api = player.Entity.Api;
			ShacklesModSystem shacklesMod = api.ModLoader.GetModSystem<ShacklesModSystem>();


			FullTrackData trackData = shacklesMod.Tracker.GetTrackData(player.PlayerUID);
			if (trackData != null)
			{
				string lastFuelerUID = trackData.LastFuelerUID;
				if (lastFuelerUID != null)
				{
					return TextCommandResult.Success("The player who last fueled your shackle is: " + api.World.PlayerByUid(lastFuelerUID).PlayerName + "!");
				}
			}
			return TextCommandResult.Error("No log exists, unshackled?");
		}

		public static TextCommandResult AdminFreePrisoner(TextCommandCallingArgs args)
		{
			var player = args.Caller.Player;
			var api = player.Entity.Api;
			ShacklesModSystem shacklesMod = api.ModLoader.GetModSystem<ShacklesModSystem>();

			int num = 0;
			try
			{
				string text = args[0].ToString();
				if (text != null)
				{
					num = 28;
					IServerPlayer serverPlayer = null;
					_ = player.InventoryManager?.ActiveHotbarSlot;
					IPlayer[] allPlayers = player.Entity.World.AllPlayers;
					foreach (IPlayer player2 in allPlayers)
					{
						num = 34;
						if (player2.PlayerName == text)
						{
							num = 37;
							serverPlayer = player2 as IServerPlayer;
							break;
						}
					}
					if (serverPlayer != null && serverPlayer.PlayerUID != null && shacklesMod.Tracker.GetTrackData(serverPlayer.PlayerUID) != null)
					{
						num = 47;
						shacklesMod.Prison.FreePlayer(serverPlayer.PlayerUID, shacklesMod.Tracker.GetTrackData(serverPlayer.PlayerUID).Slot);
						return TextCommandResult.Success("Player " + serverPlayer.PlayerName + " Freed.");
					}
					else
					{
						return TextCommandResult.Error("Player \"" + text + "\" does not exist!");
					}
				}
				else
				{
					return TextCommandResult.Error("Please provide a valid player name.");

				}
			}
			catch (Exception ex)
			{
				player.Entity.World.Logger.Debug("[ShackleGear] Exception thrown after: " + num);
				player.Entity.World.Logger.Debug("[ShackleGear] Ex: " + ex);
			}

			return TextCommandResult.Error("Failed, Unkown Reason!");
		}

		public static TextCommandResult AdminShackle(TextCommandCallingArgs args)
		{
			IPlayer adminPlayer = args.Caller.Player;
			ShacklesModSystem shackleMod = adminPlayer.Entity.Api.ModLoader.GetModSystem<ShacklesModSystem>();

			try
			{
				string text = args[0].ToString();
				if (text != null)
				{
					IServerPlayer serverPlayer = null;
					ItemSlot itemSlot = adminPlayer.InventoryManager?.ActiveHotbarSlot;
					IPlayer[] allPlayers = adminPlayer.Entity.World.AllPlayers;
					foreach (IPlayer player2 in allPlayers)
					{
						if (player2.PlayerName == text)
						{
							serverPlayer = player2 as IServerPlayer;
							break;
						}
					}
					if (serverPlayer != null && serverPlayer.PlayerUID != null)
					{
						if (itemSlot?.Itemstack?.Item is ItemShackle)
						{
							if (shackleMod.Prison.TryImprisonPlayer(serverPlayer, adminPlayer, itemSlot))
							{
								return TextCommandResult.Success("Player " + serverPlayer.PlayerName + " Shackled.");
							}
							else
							{
								return TextCommandResult.Error("Not holding a ShackleGear.");
							}
						}
					}
					else
					{
						return TextCommandResult.Error("Player \"" + text + "\" does not exist.");
					}
				}
				else
				{
					return TextCommandResult.Error("Please provide a valid player name.");
				}
			}
			catch (Exception ex)
			{

				adminPlayer.Entity.World.Logger.Debug("[ShackleGear] Ex: " + ex);
			}

			return TextCommandResult.Error("Failed! Reason Unknown");
		}
	}
}
