using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace shackles
{
    [JsonObject(/*Could not decode attribute arguments.*/)]
    internal class TrackData
    {
        [JsonProperty(/*Could not decode attribute arguments.*/)]
        public SlotReference SlotReference { get; set; }

        [JsonProperty(/*Could not decode attribute arguments.*/)]
        public BlockPos LastPos { get; set; }

        [JsonProperty(/*Could not decode attribute arguments.*/)]
        public string PrisonerUID { get; set; }

        [JsonProperty(/*Could not decode attribute arguments.*/)]
        public string LastHolderUID { get; set; }

        [JsonProperty(/*Could not decode attribute arguments.*/)]
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
