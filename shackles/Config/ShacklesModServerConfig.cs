using Newtonsoft.Json;
using System;
using Vintagestory.API.Common;

namespace Shackles.Config
{
	[JsonObject()]
	public class ShacklesModServerConfig
	{
		[JsonIgnore]
		private ICoreAPI sapi;

		[JsonProperty]
		public float shackleBurnTimeMul = 60f;
		[JsonProperty]
		public string shackledGroup = "suvisitor";

		public ShacklesModServerConfig(ICoreAPI api)
		{
			sapi = api;
		}

		public void Save()
		{
			sapi.StoreModConfig(this, "Shackles/server.json");
		}

		public void Load()
		{
			try
			{
				ShacklesModServerConfig shacklesServerConfig = sapi.LoadModConfig<ShacklesModServerConfig>("Shackles/server.json") ?? new ShacklesModServerConfig(sapi);


				shackleBurnTimeMul = shacklesServerConfig.shackleBurnTimeMul;
				shackledGroup = shacklesServerConfig.shackledGroup;

			}
			catch (Exception ex)
			{
				sapi.Logger.Error("Malformed ModConfig file Shackles/server.json, Exception: \n {0}", ex.StackTrace);
			}
		}
	}
}
