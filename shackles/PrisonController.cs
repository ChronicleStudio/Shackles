using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

namespace shackles
{
    internal class PrisonController
    {
        private ICoreServerAPI sapi;

        private ShackleGearTrackerModSystem tracker;

        private ShackleGearModSystem shackleGear;

        public PrisonController(ICoreServerAPI sapi)
        {
            this.sapi = sapi;
            tracker = sapi.ModLoader.GetModSystem<ShackleGearTrackerModSystem>();
            shackleGear = sapi.ModLoader.GetModSystem<ShackleGearModSystem>();
        }

        public BlockPos GetShacklePos(string uid)
        {
            return tracker.GetTrackData(uid)?.LastPos;
        }

        public void FreePlayer(string uid, ItemSlot slot, bool destroy = true, BlockPos brokenAt = null)
        {
            
            if (sapi == null)
            {
                return;
            }
            if (sapi.World.PlayerByUid(uid) is IServerPlayer serverPlayer)
            {
                string role = "";
                serverPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "You've been freed!", EnumChatType.Notification);
                if (shackleGear.oldRoles.TryGetValue(serverPlayer.PlayerUID, out role))
                {
                    sapi.Permissions.SetRole(serverPlayer, role);
                    shackleGear.oldRoles.Remove(serverPlayer.PlayerUID);
                    shackleGear.SaveRoleDataToDB();
                }
                else
                {
                    sapi.Permissions.SetRole(serverPlayer, "suplayer");                    
                }
            }
            
            //(sapi.PlayerData.GetPlayerDataByUid(uid) as ServerPlayerData).SetRole((sapi.World as ServerMain).Config.RolesByCode["suplayer"]);
            ITreeAttribute treeAttribute = slot?.Itemstack?.Attributes;
            if (treeAttribute != null)
            {
                Vec3d spawnFromAttributes = GetSpawnFromAttributes(treeAttribute);
                ServerMain serverMain = sapi.World as ServerMain;
                if (serverMain.GetWorldPlayerData(uid) is ServerWorldPlayerData serverWorldPlayerData)
                {
                    IServerPlayer player = sapi.World.PlayerByUid(uid) as IServerPlayer;
                    
                    if(player != null)
                    {
                        player.SetSpawnPosition(new PlayerSpawnPos
                        {
                            x = (int)spawnFromAttributes.X,
                            y = (int)spawnFromAttributes.Y,
                            z = (int)spawnFromAttributes.Z
                        });
                    }
                    
                    //serverWorldPlayerData.set_SpawnPosition(new PlayerSpawnPos
                    //{
                    //    x = (int)spawnFromAttributes.X,
                    //    y = (int)spawnFromAttributes.Y,
                    //    z = (int)spawnFromAttributes.Z
                    //});
                }
                //else
                //{
                //    byte[] playerData = serverMain.GetField<ChunkServerThread>("chunkThread").GetField<GameDatabase>("gameDatabase").GetPlayerData(uid);
                //    if (playerData != null)
                //    {
                //        try
                //        {
                //            ServerWorldPlayerData serverWorldPlayerData2 = SerializerUtil.Deserialize<ServerWorldPlayerData>(playerData);
                //            serverWorldPlayerData2.Init(serverMain);
                //            serverWorldPlayerData2.set_SpawnPosition(new PlayerSpawnPos
                //            {
                //                x = (int)spawnFromAttributes.X,
                //                y = (int)spawnFromAttributes.Y,
                //                z = (int)spawnFromAttributes.Z
                //            });
                //            serverMain.PlayerDataManager.WorldDataByUID[uid] = serverWorldPlayerData2;
                //        }
                //        catch (Exception)
                //        {
                //        }
                //    }
                //}
            }
            if (slot != null)
            {
                if (destroy)
                {
                    if (brokenAt != null)
                    {
                        sapi.World.PlaySoundAt(new AssetLocation("sounds/block/glass"), (double)brokenAt.X + 0.5, brokenAt.Y, (double)brokenAt.Z + 0.5);
                        sapi.World.SpawnCubeParticles(brokenAt.ToVec3d().Add(0.5, 0.0, 0.5), slot.Itemstack, 1f, 32);
                    }
                    slot.TakeOutWhole();
                }
                slot.MarkDirty();
            }
            tracker.TryRemoveItemFromTrack(uid);
            if (shackleGear.TrackerIDs.ContainsKey(uid))
            {
                sapi.Event.UnregisterGameTickListener(shackleGear.TrackerIDs[uid]);
            }
            ClearCellSpawn(uid);
        }

        public void MovePlayer(string uid, BlockPos pos)
        {
            IServerPlayer serverPlayer = sapi.World.PlayerByUid(uid) as IServerPlayer;
            if (pos != null)
            {
                serverPlayer?.Entity.TeleportTo(pos.X, pos.Y, pos.Z);
            }
        }

        public void MoveToCell(Entity entity)
        {
            string uid = ((entity as EntityPlayer)?.Player as IServerPlayer)?.PlayerUID;
            MovePlayer(uid, GetCellSpawn(entity));
        }

        public void MoveToCell(string uid)
        {
            if (uid != null)
            {
                MovePlayer(uid, GetCellSpawn(uid));
            }
        }

        public void ClearCellSpawn(string uid)
        {
            if (sapi.World.PlayerByUid(uid) is IServerPlayer serverPlayer)
            {
                serverPlayer.Entity.WatchedAttributes.RemoveAttribute("shackled_cellX");
                serverPlayer.Entity.WatchedAttributes.RemoveAttribute("shackled_cellY");
                serverPlayer.Entity.WatchedAttributes.RemoveAttribute("shackled_cellZ");
            }
        }

        public void SetCellSpawn(string uid, BlockPos pos)
        {
            IServerPlayer serverPlayer = sapi.World.PlayerByUid(uid) as IServerPlayer;
            if (pos != null)
            {
                serverPlayer?.Entity.WatchedAttributes.SetVec3i("shackled_cell", pos.ToVec3i());
            }
        }

        public BlockPos GetCellSpawn(Entity entity)
        {
            return entity?.WatchedAttributes?.GetVec3i("shackled_cell")?.AsBlockPos;
        }

        public BlockPos GetCellSpawn(string uid)
        {
            return GetCellSpawn((sapi.World.PlayerByUid(uid) as IServerPlayer)?.Entity);
        }

        public void SetSpawnInAttributes(ITreeAttribute attribs, IServerPlayer player)
        {
            if (!attribs.HasAttribute("pearled_x"))
            {
                FuzzyEntityPos spawnPosition = player.GetSpawnPosition(consumeSpawnUse: false);
                attribs.SetDouble("pearled_x", spawnPosition.X);
                attribs.SetDouble("pearled_y", spawnPosition.Y);
                attribs.SetDouble("pearled_z", spawnPosition.Z);
            }
        }

        public Vec3d GetSpawnFromAttributes(ITreeAttribute attribs)
        {
            return new Vec3d(attribs.GetDouble("pearled_x"), attribs.GetDouble("pearled_y"), attribs.GetDouble("pearled_z"));
        }

        public bool TryImprisonPlayer(IServerPlayer prisoner, IPlayer killer, ItemSlot slot)
        {            
            ITreeAttribute treeAttribute = slot?.Itemstack?.Attributes;
            if (treeAttribute == null)
            {
                return false;
            }
            long ticks = DateTime.UtcNow.Ticks;
            ShackleGearTrackerModSystem modSystem = sapi.ModLoader.GetModSystem<ShackleGearTrackerModSystem>();
            if (modSystem.IsShackled(prisoner))
            {
                return false;
            }
            treeAttribute.SetString("pearled_uid", prisoner.PlayerUID);
            treeAttribute.SetString("pearled_name", prisoner.PlayerName);
            treeAttribute.SetLong("pearled_timestamp", ticks);
            SetSpawnInAttributes(treeAttribute, prisoner);
            Vec3d xYZ = prisoner.Entity.ServerPos.XYZ;
            prisoner.SetSpawnPosition(new PlayerSpawnPos
            {
                x = xYZ.XInt,
                y = xYZ.YInt,
                z = xYZ.ZInt
            });
            if (!modSystem.TryRemoveItemFromTrack(prisoner))
            {
                modSystem.AddItemToTrack(new TrackData(GenSlotReference(slot), killer.Entity.ServerPos.AsBlockPos, prisoner.PlayerUID, killer.PlayerUID, killer.PlayerUID));
            }
            sapi.ModLoader.GetModSystem<ShackleGearModSystem>().RegisterPearlUpdate(prisoner);
            prisoner.SendMessage(GlobalConstants.GeneralChatGroup, "You've been shackled!", EnumChatType.Notification);
            slot.MarkDirty();
            (sapi.World as ServerMain).EventManager.TriggerPlayerRespawn(prisoner);
            sapi.World.PlaySoundAt(new AssetLocation("sounds/wearable/chain1"), prisoner, null, randomizePitch: false, 20f, 12f);
            IInventory inv = prisoner.Entity.GearInventory;
            ItemStack stack = new ItemStack(prisoner.Entity.Api.World.GetItem(new AssetLocation("game", "clothes-arm-prisoner-binds")));
            if (stack != null)
            {
                Enum.TryParse<EnumCharacterDressType>(stack.ItemAttributes["clothescategory"].AsString(), ignoreCase: true, out var dresstype);

                inv[(int)dresstype].Itemstack = stack;
                inv[(int)dresstype].MarkDirty();
            }


            return true;
        }

        public SlotReference GenSlotReference(ItemSlot slot)
        {
            return new SlotReference(slot.Inventory.GetSlotId(slot), slot.Inventory.InventoryID);
        }
    }
}
