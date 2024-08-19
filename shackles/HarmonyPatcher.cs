using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;

namespace shackles
{
    internal class HarmonyPatcher : ModSystem
    {
        private const string patchCode = "ModSystem";

        public string sidedPatchCode;

        public Harmony harmonyInstance;

        private static bool harmonyPatched;

        public override void Start(ICoreAPI api)
        {
            if (harmonyPatched)
            {
                return;
            }
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            string arg = base.Mod?.Info.Name ?? executingAssembly.GetCustomAttribute<ModInfoAttribute>()?.Name ?? "Null";
            sidedPatchCode = string.Format("{0}.{1}.{2}", arg, "ModSystem", api.Side);
            harmonyInstance = new Harmony(sidedPatchCode);
            harmonyInstance.PatchAll();
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            foreach (MethodBase patchedMethod in harmonyInstance.GetPatchedMethods())
            {
                if (dictionary.ContainsKey(GeneralExtensions.FullDescription(patchedMethod)))
                {
                    dictionary[GeneralExtensions.FullDescription(patchedMethod)]++;
                }
                else
                {
                    dictionary[GeneralExtensions.FullDescription(patchedMethod)] = 1;
                }
            }
            StringBuilder stringBuilder = new StringBuilder($"{arg}: Harmony Patched Methods: ").AppendLine();
            stringBuilder.AppendLine("[");
            foreach (KeyValuePair<string, int> item in dictionary)
            {
                stringBuilder.AppendLine($"  {item.Value}: {item.Key}");
            }
            stringBuilder.Append("]");
            api.Logger.Notification(stringBuilder.ToString());
            harmonyPatched = true;
        }

        public override void Dispose()
        {
            Harmony obj = harmonyInstance;
            if (obj != null)
            {
                obj.UnpatchAll(sidedPatchCode);
            }
            harmonyPatched = false;
        }
    }
}
