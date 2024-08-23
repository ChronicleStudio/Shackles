using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Shackles.Data
{
	public class FullTrackData
	{
		private TrackData trackData;

		private ICoreServerAPI api;

		public ItemStack ItemStack => Slot?.Itemstack;

		public IServerPlayer Prisoner => (IServerPlayer)api.World.PlayerByUid(PrisonerUID);

		public IServerPlayer LastHolder => (IServerPlayer)api.World.PlayerByUid(LastHolderUID);

		public Vec3i LastChunkPos => new Vec3i(trackData.LastPos.X / Chunksize, trackData.LastPos.Y / Chunksize, trackData.LastPos.Z / Chunksize) ?? null;

		private int Chunksize => api.World.BlockAccessor.ChunkSize;

		public bool IsChunkForceLoaded { get; private set; }

		public SlotReference SlotReference
		{
			get
			{
				return trackData.SlotReference;
			}
			set
			{
				trackData.SlotReference = value;
			}
		}

		public BlockPos LastPos
		{
			get
			{
				return trackData.LastPos;
			}
			set
			{
				trackData.LastPos = value;
			}
		}

		public string PrisonerUID
		{
			get
			{
				return trackData.PrisonerUID;
			}
			set
			{
				trackData.PrisonerUID = value;
			}
		}

		public string LastHolderUID
		{
			get
			{
				return trackData.LastHolderUID;
			}
			set
			{
				trackData.LastHolderUID = value;
			}
		}

		public string LastFuelerUID
		{
			get
			{
				return trackData.LastFuelerUID;
			}
			set
			{
				trackData.LastFuelerUID = value;
			}
		}

		public bool IsNull => trackData == null;

		public ItemSlot Slot
		{
			get
			{
				ItemSlot result = null;
				if (IsChunkForceLoaded)
				{
					result = ((LastHolder?.InventoryManager?.Inventories == null || !LastHolder.InventoryManager.Inventories.ContainsKey(trackData.SlotReference.InventoryID)) ? (api.World.BlockAccessor.GetBlockEntity(trackData.LastPos) as IBlockEntityContainer)?.Inventory?[trackData.SlotReference.SlotID] : LastHolder.InventoryManager.Inventories[trackData.SlotReference.InventoryID][trackData.SlotReference.SlotID]);
				}
				return result;
			}
		}

		public FullTrackData(TrackData trackData, ICoreServerAPI api)
		{
			this.trackData = trackData;
			this.api = api;
		}

		public void SetLocation(int x, int y, int z)
		{
			LastPos.X = x;
			LastPos.Y = y;
			LastPos.Z = z;
		}

		public void SetLocation(BlockPos pos)
		{
			SetLocation(pos.X, pos.Y, pos.Z);
		}

		public void LoadMyChunk()
		{
			if (!IsChunkForceLoaded)
			{
				api.WorldManager.LoadChunkColumnPriority(LastChunkPos.X, LastChunkPos.Z, new ChunkLoadOptions
				{
					KeepLoaded = true,
					OnLoaded = delegate
					{
						IsChunkForceLoaded = true;
						api.World.BlockAccessor.GetBlockEntity(trackData.LastPos)?.Initialize(api);
					}
				});
			}
		}

		public void MarkUnloadable()
		{
			api.WorldManager.LoadChunkColumnPriority(LastChunkPos.X, LastChunkPos.Z, new ChunkLoadOptions
			{
				KeepLoaded = false
			});
			api.World.Logger.Debug("[ShackleGear] Chunk Column Marked Unloadable: " + LastChunkPos.X + ", " + LastChunkPos.Z);
			IsChunkForceLoaded = false;
		}
	}
}
