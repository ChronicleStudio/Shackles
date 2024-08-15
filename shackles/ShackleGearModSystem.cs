using HarmonyLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace shackles
{
    internal class ShackleGearModSystem : ModSystem
    {
        private ICoreAPI api;

        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        public Dictionary<string, long> TrackerIDs = new Dictionary<string, long>();
        
        public Dictionary<string, string> oldRoles = new Dictionary<string, string>();

        private Type dummyPlayerType;

        internal ShacklesServerConfig shackleServerConfig;

        public PrisonController Prison { get; private set; }

        private ShackleGearTrackerModSystem Tracker => api.ModLoader.GetModSystem<ShackleGearTrackerModSystem>();
              

        public void RegisterServerCommands(ICoreServerAPI api)
        {            
            var parsers = api.ChatCommands.Parsers;
                       

            api.ChatCommands.GetOrCreate("sg")
                .RequiresPlayer()
                .RequiresPrivilege(Privilege.chat)                              
                .BeginSubCommand("fuellog")
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.chat)
                    .WithDescription("Shows who last fueled your shackle-gear")
                    .HandleWith(FuelLog)
                    .EndSubCommand()
                .BeginSubCommand("admin")
                    .BeginSubCommand("free")
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.controlserver)
                        .WithDescription("Frees a shackled player")
                        .WithArgs(
                            parsers.Word("playername")
                        )
                        .HandleWith(AdminFreePrisoner)
                        .EndSubCommand()
                    .BeginSubCommand("shackle")
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.controlserver)
                        .WithDescription("Shackle a player.")
                        .WithArgs(
                            parsers.Word("playername")
                        )
                        .HandleWith(AdminShackle)
                        .EndSubCommand()
                    .EndSubCommand();

        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;            
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            Prison = new PrisonController(sapi);
            RegisterServerCommands(sapi);
            dummyPlayerType = AccessTools.TypeByName("dummyplayer.src.EntityClonePlayer");
            api.Event.OnEntityDeath += OnEntityDeath;
            api.Event.PlayerDeath += OnPlayerDeath;
            api.Event.PlayerDisconnect += EventOnPlayerDisconnect;                        
            
            api.Event.PlayerJoin += delegate (IServerPlayer player)
            {
                RegisterPearlUpdate(player);
                
            };
            shackleServerConfig = new ShacklesServerConfig(api);
            shackleServerConfig.Load();
            shackleServerConfig.Save();
            LoadRoleDataFromDB();     
        }
                      

        private void OnEntityDeath(Entity entity, DamageSource damageSource)
        {
            if (entity is EntityPlayer entityPlayer)
            {
                OnPlayerDeath((IServerPlayer)entityPlayer.Player, damageSource);
            }
            if (dummyPlayerType != null && entity.GetType() == dummyPlayerType)
            {
                IServerPlayer byplayer = (IServerPlayer)sapi.World.PlayerByUid(entity.GetField<string>("sourceEntityUID"));
                OnPlayerDeath(byplayer, damageSource);
            }
        }

        public void RegisterPearlUpdate(IServerPlayer player)
        {
            FullTrackData data = Tracker.GetTrackData(player.PlayerUID);
            if (!(data?.LastPos != null) || player == null || player.PlayerUID == null)
            {
                return;
            }
            if(player.Role.Code != shackleServerConfig.shackledGroup && !oldRoles.ContainsKey(player.PlayerUID))
            {
                oldRoles.Add(player.PlayerUID, player.Role.Code);
                
            }

            sapi.Permissions.SetRole(player, shackleServerConfig.shackledGroup);

            
            string uid = player.PlayerUID;
            TrackerIDs[uid] = sapi.Event.RegisterGameTickListener(delegate
            {
                try
                {
                    data.LoadMyChunk();
                    if (data.IsChunkForceLoaded && data.ItemStack?.Item is ItemShackle)
                    {
                        ((ItemShackle)data.ItemStack.Item).UpdateFuelState(sapi.World, data.Slot);
                    }
                }
                catch (Exception)
                {
                    data.MarkUnloadable();
                    sapi.Event.UnregisterGameTickListener(TrackerIDs[uid]);
                }
            }, 500);
            SaveRoleDataToDB();
        }

        public void OnPlayerDeath(IServerPlayer byplayer, DamageSource damagesource)
        {
            if (!(damagesource?.SourceEntity is EntityPlayer))
            {
                return;
            }
            IPlayer killer = sapi.World.PlayerByUid(((EntityPlayer)damagesource.SourceEntity).PlayerUID);
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

        

        public void EventOnPlayerDisconnect(IServerPlayer byplayer)
        {
            if (TrackerIDs.ContainsKey(byplayer.PlayerUID))
            {
                sapi.Event.UnregisterGameTickListener(TrackerIDs[byplayer.PlayerUID]);
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
                        sapi.World.SpawnItemEntity(itemstack, byplayer.Entity.ServerPos.XYZ);
                    }
                }
            }
        }
                

        private TextCommandResult FuelLog(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;

            FullTrackData trackData = Tracker.GetTrackData(player.PlayerUID);
            if (trackData != null)
            {
                string lastFuelerUID = trackData.LastFuelerUID;
                if (lastFuelerUID != null)
                {
                    return TextCommandResult.Success("The player who last fueled your shackle is: " + sapi.World.PlayerByUid(lastFuelerUID).PlayerName + " With the UID of: " + lastFuelerUID);                    
                }
            }            
            return TextCommandResult.Error("No log exists, unshackled?");  
        }

        private TextCommandResult AdminFreePrisoner(TextCommandCallingArgs args)
        {
            var player = args.Caller.Player;

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
                    if (serverPlayer != null && serverPlayer.PlayerUID != null && Tracker.GetTrackData(serverPlayer.PlayerUID) != null)
                    {
                        num = 47;
                        Prison.FreePlayer(serverPlayer.PlayerUID, Tracker.GetTrackData(serverPlayer.PlayerUID).Slot);
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

        private TextCommandResult AdminShackle(TextCommandCallingArgs args)
        {
            IPlayer adminPlayer = args.Caller.Player;

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
                            if (Prison.TryImprisonPlayer(serverPlayer, adminPlayer, itemSlot))
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

        public void SaveRoleDataToDB()
        {
            sapi.WorldManager.SaveGame.StoreData("shacklegear_roledata", JsonUtil.ToBytes(oldRoles));
        }

        public void LoadRoleDataFromDB()
        {
            byte[] data = sapi.WorldManager.SaveGame.GetData("shacklegear_roledata");
            if (data != null)
            {
                foreach (KeyValuePair<string, string> roledata in JsonUtil.FromBytes<Dictionary<string, string>>(data))
                {
                    oldRoles.Add(roledata.Key, roledata.Value);
                }
                return;
            }
            SaveRoleDataToDB();
        }
       

        

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

        }

    }
}
