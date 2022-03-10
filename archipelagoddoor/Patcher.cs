using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;

namespace ddoor.ap
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    internal class Patcher : BaseUnityPlugin
    {
        public const string pluginGuid = "epsis.archipelago.ddoor";
        public const string pluginName = "Archipelago for Death's Door";
        public const string pluginVersion = "0.0.0.1";

        public void Awake()
        {
            Logger.LogInfo("Started plugin");
        }

        public static void Patch()
        {
            ApState.Init();

            var harmony = new Harmony("com.epsis.ddoor.archipelago");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
