using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;
using UnityEngine.SceneManagement;

namespace ap.ddoor
{
    internal class DataStore
    {
        public struct Location
        {
            public int Id;
            public string Name;
            public Vector3 Position;
        }

        private List<InventoryItem> Items { get; set; }
        private List<Location> Locations { get; set; }
        private Scene[] Scenes { get; set; }
        private string[] SceneNames { get; set; }

        public DataStore()
        {
            Patcher.logger.LogInfo("Loading DataStore...");
            Items = LoadItems();
            Locations = new List<Location>(); //TODO: GetLocations
            Scenes = LoadScenes();
            SceneNames = LoadSceneNames();

            Patcher.logger.LogInfo($"Found {Items.Count} items, {Locations.Count} locations and {Scenes.Length} scenes.");
            
            LogAllScenes();

            foreach (var sceneName in SceneNames)
            {
                Patcher.logger.LogInfo(sceneName);
            }

            //foreach (var item in Scenes[2].GetRootGameObjects())
            //{
            //    foreach (var component in item.GetComponents(typeof(Component)))
            //    {
            //        Patcher.logger.LogInfo(component.ToString());
            //    }
            //}

            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;
        }

        private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1)
        {
            LogAllScenes();
        }
        
        private void LogAllScenes()
        {
            foreach (var item in Scenes)
            {
                Patcher.logger.LogInfo($"Scene: {item.name} ({item.rootCount}) - {item.GetRootGameObjects().Length} root items");
            }
        }

        private static List<InventoryItem> LoadItems() => ItemDatabase.instance.data.ToList();

        private static List<Location> GetLocations()
        {
            throw new NotImplementedException();
        }

        private static Scene[] LoadScenes()
        {
            var scenes = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                scenes.Add(SceneManager.GetSceneAt(i));
            }

            return scenes.ToArray();
        }

        private static string[] LoadSceneNames()
        {
            var sceneCount = SceneManager.sceneCountInBuildSettings;
            Patcher.logger.LogInfo(sceneCount);
            var scenes = new string[sceneCount];
            for (var i = 0; i < sceneCount; i++)
            {
                scenes[i] = System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                Patcher.logger.LogInfo(scenes[i]);
            }

            return scenes;
        }

        public InventoryItem GetRandomItem()
        {
            var rnd = new Random();
            return Items[rnd.Next(0, Items.Count)];
        }

        public string GetRandomItemId()
        {
            var rnd = new Random();
            return Items[rnd.Next(0, Items.Count)].id;
        }

        public GameObject[] GetSceneGameObjects(string scene)
        {
            return SceneManager.GetSceneByName(scene).GetRootGameObjects();
        }
    }
}
