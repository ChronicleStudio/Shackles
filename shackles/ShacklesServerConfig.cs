using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace shackles
{
    [JsonObject()]
    internal class ShacklesServerConfig
    {
        [JsonIgnore]
        private ICoreAPI sapi;
                   
        [JsonProperty]
        public float shackleBurnTimeMul = 60f;
        [JsonProperty]
        public string shackledGroup = "suvisitor";

        public ShacklesServerConfig(ICoreAPI api)
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
                ShacklesServerConfig shacklesServerConfig = sapi.LoadModConfig<ShacklesServerConfig>("Shackles/server.json") ?? new ShacklesServerConfig(sapi);
                

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
