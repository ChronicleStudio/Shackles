using System;
using System.Collections.Generic;
using HarmonyLib;
using Shackles.Commands;
using Shackles.Config;
using Shackles.EntityBehaviors;
using Shackles.Events;
using Shackles.Items;
using Shackles.ShackleSystems;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Shackles
{
    internal class ShacklesModSystem : ModSystem
    {
        private ICoreAPI api;

        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        public Dictionary<string, long> TrackerIDs = new Dictionary<string, long>();

        public Dictionary<string, string> oldRoles = new Dictionary<string, string>();

        public Type dummyPlayerType;

        internal ShacklesModServerConfig config;

        public PrisonController Prison { get; private set; }

        public ShackleTrackerModSystem Tracker => api.ModLoader.GetModSystem<ShackleTrackerModSystem>();


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
                    .HandleWith(ShackleCommands.FuelLog)
                    .EndSubCommand()
                .BeginSubCommand("admin")
                    .BeginSubCommand("free")
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.controlserver)
                        .WithDescription("Frees a shackled player")
                        .WithArgs(
                            parsers.Word("playername")
                        )
                        .HandleWith(ShackleCommands.AdminFreePrisoner)
                        .EndSubCommand()
                    .BeginSubCommand("shackle")
                        .RequiresPlayer()
                        .RequiresPrivilege(Privilege.controlserver)
                        .WithDescription("Shackle a player.")
                        .WithArgs(
                            parsers.Word("playername")
                        )
                        .HandleWith(ShackleCommands.AdminShackle)
                        .EndSubCommand()
                    .EndSubCommand();

        }

		public override void StartPre(ICoreAPI api)
		{
			config = new ShacklesModServerConfig(api);
			config.Load();
			config.Save();

			base.StartPre(api);
		}

		public override void Start(ICoreAPI api)
        {
			api.RegisterItemClass("ItemShackle", typeof(ItemShackle));
			api.RegisterEntityBehaviorClass("gearfinder", typeof(EntityBehaviorShackleFinder));

			this.api = api;            

			base.Start(api);
		}

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            Prison = new PrisonController(sapi);
            RegisterServerCommands(sapi);
            dummyPlayerType = AccessTools.TypeByName("dummyplayer.src.EntityClonePlayer");
            api.Event.OnEntityDeath += ShackleEvents.OnEntityDeath;
            api.Event.PlayerDeath += ShackleEvents.OnPlayerDeath;
            api.Event.PlayerDisconnect += ShackleEvents.EventOnPlayerDisconnect;

            api.Event.PlayerJoin += delegate (IServerPlayer player)
            {
                ShackleEvents.RegisterPearlUpdate(player);

            };            
            LoadRoleDataFromDB();
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
                
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            capi = api;

        }

    }
}
