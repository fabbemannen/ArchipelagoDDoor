using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;

namespace ap.ddoor
{
    internal class Randomizer
    {
        private static DataStore dataStore;

        public static void Init()
        {
            //LogCurrentGameObjects();
        }

        public static void OnNewScene(string scene)
        {
            if (scene == "TitleScreen") return;

            Patcher.logger.LogInfo("OnNewScene - Scene: " + scene);

            if (string.IsNullOrEmpty(scene))
            {
                Patcher.logger.LogWarning("Scene name can't be null or empty");
                return;
            }

            if (dataStore == null)
            {
                dataStore = new DataStore();
            }

            var sceneGameObjects = dataStore.GetSceneGameObjects(scene);
            Patcher.logger.LogInfo("sceneGameObjects: " + sceneGameObjects.Length);
            var dropItemGameObjects = FindObjectsWithComponent(typeof(DropItem), sceneGameObjects);
            Patcher.logger.LogInfo("dropItemGameObjects: " + dropItemGameObjects.Length);

            foreach (var dropItemGameObject in dropItemGameObjects)
            {
                ReplaceDropItem(dropItemGameObject.GetComponent<DropItem>());
            }
        }

        private static void ReplaceDropItem(DropItem dropItem)
        {
            if (dataStore == null)
            {
                dataStore = new DataStore();
            }

            //TODO: Logic based on location

            InventoryItem? randomItem = null;
            GameObject? randomPrefab = null;
            while (randomPrefab == null)
            {
                randomItem = dataStore.GetRandomItem();
                randomPrefab = randomItem.prefab;
                if (randomPrefab != null)
                {
                    if (randomPrefab.name != "WEAPON_Daggers")
                    {
                        randomPrefab = null;
                    }
                }
            }

            if (dropItem.gameObject.TryGetComponent<Renderer>(out _))
            {
                dropItem.gameObject.GetComponent<Renderer>().enabled = false;
                Patcher.logger.LogWarning($"Replaced {dropItem.itemId} with {randomItem?.id}");
            }
            else
            {
                Patcher.logger.LogWarning($"Failed to replace {dropItem.itemId} with {randomItem?.id}. Iterating through components:");

                var count = 0;
                //foreach (var component in dropItem.gameObject.GetComponents<Component>())
                //{
                //    count++;
                //    Patcher.logger.LogInfo(component.ToString());
                //}
                //Patcher.logger.LogWarning($"Found {count} components in {dropItem.itemId}. Checking parent:");

                //count = 0;
                //foreach (var component in dropItem.gameObject.GetComponentsInParent<Component>())
                //{
                //    count++;
                //    Patcher.logger.LogInfo(component.ToString());
                //}
                //Patcher.logger.LogWarning($"Found {count} parent components in {dropItem.itemId}. Checking children:");

                //count = 0;
                foreach (var component in dropItem.gameObject.GetComponentsInChildren<MeshFilter>())
                {
                    count++;
                    Patcher.logger.LogInfo(component.ToString());
                    Object.Destroy(component);
                }
                Patcher.logger.LogWarning($"Found {count} mesh filters in {dropItem.itemId}.");
            }

            var instantiatedObject = Object.Instantiate(randomPrefab);
            instantiatedObject.transform.SetPositionAndRotation(dropItem.gameObject.transform.position, dropItem.gameObject.transform.rotation);
            instantiatedObject.transform.parent = dropItem.transform;

            dropItem.itemId = randomItem?.id;
            //dropItem.itemId = dataStore.GetRandomItemId();
            //Patcher.logger.LogInfo("Picked up " + dropItem.itemId);
        }

        private static GameObject[] FindObjectsWithComponent(Type type, GameObject[] array, bool includeChildren = true)
        {
            var foundObjects = new HashSet<GameObject>();

            foreach (var item in array)
            {
                foreach (var component in item.GetComponents(type))
                {
                    foundObjects.Add(component.gameObject);
                }

                if (!includeChildren) continue;
                {
                    foreach (var component in item.GetComponentsInChildren(type))
                    {
                        foundObjects.Add(component.gameObject);
                    }
                }
            }

            return foundObjects.ToArray();
        }

        /// <summary>
        /// Log all GameObjects in the current scene
        /// </summary>
        public static void LogCurrentGameObjects()
        {
            var gameObjects = Object.FindObjectsOfType<GameObject>();
            var untagged = 0;
            var tagged = 0;

            foreach (var item in gameObjects)
            {
                if (item.tag == "Untagged")
                {
                    untagged++;
                    continue;
                }

                tagged++;

                var pos = item.transform.position;
                var posToString = $"{pos.x},{pos.y},{pos.z}";

                Patcher.logger.LogInfo(string.Format("({0}) Tag: {1}, scene: {2}, name: {3}", new object[]
                {
                    posToString,
                    item.tag,
                    item.scene.name,
                    item.name
                }));
            }

            Patcher.logger.LogInfo($"GameObjects in scene: {untagged}+{tagged}={untagged + tagged}");
        }

        /// <summary>
        /// Stops input debugging from clogging the console
        /// </summary>
        [HarmonyPatch(typeof(Buttons), "DebugLogging")]
        public class ButtonDebuggingPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                return false;
            }
        }

        /// <summary>
        /// Log all items in the ItemDatabase
        /// </summary>
        [HarmonyPatch(typeof(ItemDatabase), "Awake")]
        public class ItemDatabaseInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ItemDatabase __instance)
            {
                Patcher.logger.LogInfo("Inventory database populated, contains: " + __instance.data.Length);
                foreach (InventoryItem item in __instance.data)
                {
                    try
                    {
                        Patcher.logger.LogInfo(string.Format("id: {0}, type: {1}, stackCount: {2}, maxStack: {3}, uiImg: {4}, unlocked: {5}, currCost: {6}, specReq: {7}, prefab: {8}, position: {9}, desc: {10}", new object[]
                        {
                            item.id,
                            item.itemType,
                            item.stackCount,
                            item.maxStack,
                            item.uiImage,
                            item.unlocked,
                            item.currencyCost,
                            item.specialItemRequirement,
                            item.prefab.name,
                            item.prefab.transform.position,
                            item.description
                        }));
                    }
                    catch
                    {
                        Patcher.logger.LogInfo(string.Format("id: {0}, type: {1}, stackCount: {2}, maxStack: {3}, uiImg: {4}, unlocked: {5}, currCost: {6}, specReq: {7}, desc: {8}", new object[]
                        {
                            item.id,
                            item.itemType,
                            item.stackCount,
                            item.maxStack,
                            item.uiImage,
                            item.unlocked,
                            item.currencyCost,
                            item.specialItemRequirement,
                            item.description
                        }));
                    }
                }
                Patcher.logger.LogInfo("Inventory database populated, contains: " + __instance.data.Length);
            }
        }

        /// <summary>
        /// Randomize pickups
        /// </summary>
        [HarmonyPatch(typeof(DropItem), "Trigger")]
        public class PickupTriggerPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref DropItem __instance)
            {
                //if (dataStore == null)
                //{
                //    dataStore = new DataStore();
                //}

                ////TODO: Logic based on location
                //var randomItem = dataStore.GetRandomItem();

                //if (__instance.gameObject.TryGetComponent<MeshRenderer>(out _))
                //{
                //    var instantiatedObject = Object.Instantiate(randomItem.prefab);
                //    instantiatedObject.transform.SetParent(__instance.gameObject.transform);
                //    __instance.gameObject.GetComponent<MeshRenderer>().enabled = false;
                //    Patcher.logger.LogInfo($"Replaced {__instance.itemId} with {randomItem.id}");
                //}
                //else
                //{
                //    Patcher.logger.LogWarning($"Failed to replace {__instance.itemId} with {randomItem.id}. Iterating through components:");
                //    var count = 0;
                //    foreach (var component in __instance.gameObject.GetComponents<Component>())
                //    {
                //        count++;
                //        Patcher.logger.LogInfo(component.ToString());
                //    }
                //    Patcher.logger.LogWarning($"Found {count} components in {__instance.itemId}. Checking parent:");

                //    count = 0;
                //    foreach (var component in __instance.gameObject.GetComponentsInParent<Component>())
                //    {
                //        count++;
                //        Patcher.logger.LogInfo(component.ToString());
                //    }
                //    Patcher.logger.LogWarning($"Found {count} parent components in {__instance.itemId}. Checking children:");

                //    count = 0;
                //    foreach (var component in __instance.gameObject.GetComponentsInChildren<Component>())
                //    {
                //        if (component.GetType() == typeof(Renderer))
                //        {
                //            ((component as Renderer)!).enabled = false;
                //        }

                //        count++;
                //        Patcher.logger.LogInfo(component.ToString());
                //    }
                //    Patcher.logger.LogWarning($"Found {count} child components in {__instance.itemId}.");
                //}

                //__instance.itemId = randomItem.id;
                //__instance.itemId = dataStore.GetRandomItemId();
                Patcher.logger.LogInfo("Picked up " + __instance.itemId);
            }
        }
    }
}
