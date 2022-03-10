using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace ap.ddoor
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    internal class Patcher : BaseUnityPlugin
    {
        public const string pluginGuid = "epsis.archipelago.ddoor";
        public const string pluginName = "Death's Door AP";
        public const string pluginVersion = "0.0.0.1";
        public static ManualLogSource logger;

        private int lastCheck = DateTime.Now.Minute;
        private string lastScene = "";

        public void Awake()
        {
            logger = Logger;
            logger.LogInfo("Started plugin");
            Patch();
        }

        public static void Patch()
        {
            Randomizer.Init();

            var harmony = new Harmony("com.epsis.ddoor.archipelago");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Update()
        {
            var now = DateTime.Now;
            if (now.Second == 0 && now.Minute != lastCheck)
            {
                lastCheck = now.Minute;
                //Randomizer.LogCurrentGameObjects();
            }

            var currentScene = SceneManager.GetActiveScene();

            if (currentScene != null)
            {
                var currentSceneName = currentScene.name;
                if (!currentSceneName.IsNullOrWhiteSpace() && currentSceneName != null && lastScene != currentSceneName)
                {
                    lastScene = currentSceneName;
                    logger.LogInfo($"New scene: {currentSceneName}");
                    Randomizer.OnNewScene(currentSceneName);
                }
            }
        }
    }
}
