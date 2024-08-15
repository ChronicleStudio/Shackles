using Cairo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace shackles
{
    public class ShacklesModSystem : ModSystem
    {
        ICoreAPI _api;

        internal ShacklesServerConfig config;

        public override void StartPre(ICoreAPI api)
        {
            config = new ShacklesServerConfig(api);
            config.Load();
            config.Save();

            base.StartPre(api);
        }

        public override void Start(ICoreAPI api)
        {           
            api.RegisterItemClass("ItemShackle", typeof(ItemShackle));
            api.RegisterEntityBehaviorClass("gearfinder", typeof(EntityBehaviorGearFinder));                       

            _api = api;
                    
            base.Start(api);
        }
    }
}
