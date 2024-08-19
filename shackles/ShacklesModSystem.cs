using Vintagestory.API.Common;

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
