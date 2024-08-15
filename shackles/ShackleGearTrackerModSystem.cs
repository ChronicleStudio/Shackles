using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace shackles
{
    internal class ShackleGearTrackerModSystem : ModSystem
    {
        private ICoreServerAPI sapi;

        public Dictionary<string, TrackData> TrackedByUID { get; set; } = new Dictionary<string, TrackData>();


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Server;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            LoadTrackFromDB();
            api.Event.ServerRunPhase(EnumServerRunPhase.RunGame, delegate
            {
                SaveTrackToDB();
            });
        }

        public void AddItemToTrack(TrackData item)
        {
            TrackedByUID[item.PrisonerUID] = item;
            SaveTrackToDB();
        }

        public void SetLastFuelerUID(string shackledUID, string fuelerUID)
        {
            if (GetTrackData(shackledUID) != null)
            {
                GetTrackData(shackledUID).LastFuelerUID = fuelerUID;
            }
        }

        public string GetLastFuelerUID(string shackledUID)
        {
            return GetTrackData(shackledUID)?.LastFuelerUID;
        }

        public FullTrackData GetTrackData(string prisoneruid)
        {
            TrackedByUID.TryGetValue(prisoneruid, out var value);
            if (value == null)
            {
                return null;
            }
            return new FullTrackData(value, sapi);
        }

        public bool IsShackled(Entity entity)
        {
            return IsShackled((entity as EntityPlayer)?.Player as IServerPlayer);
        }

        public bool IsShackled(IServerPlayer player)
        {
            if (player == null)
            {
                return false;
            }
            return TrackedByUID.ContainsKey(player.PlayerUID);
        }

        public bool TryRemoveItemFromTrack(string uid)
        {
            FullTrackData trackData = GetTrackData(uid);
            if (trackData != null)
            {
                trackData.MarkUnloadable();
                TrackedByUID.Remove(uid);
                SaveTrackToDB();
                return true;
            }
            SaveTrackToDB();
            return false;
        }

        public bool TryRemoveItemFromTrack(IServerPlayer prisoner)
        {
            return TryRemoveItemFromTrack(prisoner.PlayerUID);
        }

        public void LoadTrackFromDB()
        {
            byte[] data = sapi.WorldManager.SaveGame.GetData("shacklegear_trackdata");
            if (data != null)
            {
                foreach (TrackData item in JsonUtil.FromBytes<List<TrackData>>(data))
                {
                    TrackedByUID[item.PrisonerUID] = item;
                }
                return;
            }
            SaveTrackToDB();
        }

        public void SaveTrackToDB()
        {
            sapi.WorldManager.SaveGame.StoreData("shacklegear_trackdata", JsonUtil.ToBytes(TrackedByUID.Values.ToList()));
        }
    }
}
