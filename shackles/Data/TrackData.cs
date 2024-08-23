using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Shackles.Data
{
	[JsonObject()]
	public class TrackData
	{
		[JsonProperty()]
		public SlotReference SlotReference { get; set; }

		[JsonProperty()]
		public BlockPos LastPos { get; set; }

		[JsonProperty()]
		public string PrisonerUID { get; set; }

		[JsonProperty()]
		public string LastHolderUID { get; set; }

		[JsonProperty()]
		public string LastFuelerUID { get; set; }

		public TrackData(SlotReference slotReference, BlockPos lastPos, string prisonerUID, string lastHolderUID, string lastFuelerUID)
		{
			SlotReference = slotReference;
			LastPos = lastPos;
			PrisonerUID = prisonerUID;
			LastHolderUID = lastHolderUID;
			LastFuelerUID = lastFuelerUID;
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
	}
}
