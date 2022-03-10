using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ddoor.ap
{
    public static class ApState
    {
        //public struct Location
        //{
        //    public int id;
        //    public Vector3 position;
        //}

        public enum State
        {
            Menu,
            AwaitingSync,
            InGame
        }

        /*
        public static int[] AP_VERSION = new int[] { 0, 2, 1 };

        public static string host = "";
        public static string player_name = "";
        public static string password = "";

        public static Dictionary<int, TechType> ITEM_CODE_TO_TECHTYPE = new Dictionary<int, TechType>();
        public static List<Location> LOCATIONS = new List<Location>();

        public static Dictionary<string, int> archipelago_indexes = new Dictionary<string, int>();
        public static RoomInfoPacket room_info = null;
        public static DataPackagePacket data_package = null;
        public static ConnectedPacket connected_data = null;
        public static LoginResult login_result = null;
        public static LocationInfoPacket location_infos = null;
        public static Dictionary<int, string> player_names_by_id = new Dictionary<int, string>
        {
            { 0, "Archipelago" }
        };
        public static List<TechType> unlock_queue = new List<TechType>();
        public static float unlock_dequeue_timeout = 0.0f;
        public static List<string> message_queue = new List<string>();
        public static float message_dequeue_timeout = 0.0f;
        public static State state = State.Menu;
        public static int next_item_index = 0;

        public static ArchipelagoSession session;
        public static ArchipelagoUI archipelago_ui = null;

#if DEBUG
        public static string InspectGameObject(GameObject gameObject)
        {
            string msg = gameObject.transform.position.ToString().Trim() + ": ";

            var tech_tag = gameObject.GetComponent<TechTag>();
            if (tech_tag != null)
            {
                msg += "(" + tech_tag.type.ToString() + ")";
            }

            Component[] components = gameObject.GetComponents(typeof(Component));
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var component_name = components[i].ToString().Split('(').GetLast();
                component_name = component_name.Substring(0, component_name.Length - 1);

                msg += component_name;

                if (component_name == "ResourceTracker")
                {
                    var techTypeMember = typeof(ResourceTracker).GetField("techType", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                    var techType = (TechType)techTypeMember.GetValue(component);
                    msg += $"({techType.ToString()},{((ResourceTracker)component).overrideTechType.ToString()})";
                }

                msg += ", ";
            }

            return msg;
        }
#endif
        */

        public static void Init()
        {

        }

        /*
        static public void Init()
        {
            // Load items.json
            {
                var reader = File.OpenText("QMods/Archipelago/items.json");
                var content = reader.ReadToEnd();
                var json = new JSONObject(content);
                reader.Close();

                foreach (var item_json in json)
                {
                    ITEM_CODE_TO_TECHTYPE[(int)item_json.GetField("id").i] =
                        (TechType)Enum.Parse(typeof(TechType), item_json.GetField("tech_type").str);
                }
            }

            // Load locations.json
            {
                var reader = File.OpenText("QMods/Archipelago/locations.json");
                var content = reader.ReadToEnd();
                var json = new JSONObject(content);
                reader.Close();

                foreach (var location_json in json)
                {
                    Location location = new Location();
                    location.id = (int)location_json.GetField("id").i;
                    location.position = new Vector3(
                        location_json.GetField("position").GetField("x").f,
                        location_json.GetField("position").GetField("y").f,
                        location_json.GetField("position").GetField("z").f
                    );
                    LOCATIONS.Add(location);
                }
            }
        }

        public static void Session_ErrorReceived(Exception e, string message)
        {
            Debug.LogError(message);
            if (e != null) Debug.LogError(e.ToString());
            if (session != null)
            {
                session.Socket.Disconnect();
                session = null;
            }
        }

        public static void Session_PacketReceived(ArchipelagoPacketBase packet)
        {
            //ErrorMessage.AddError("Incoming Packet: " + packet.PacketType.ToString());
            Debug.Log("Incoming Packet: " + packet.PacketType.ToString());
            switch (packet.PacketType)
            {
                case ArchipelagoPacketType.RoomInfo:
                    {
                        room_info = packet as RoomInfoPacket;
                        updatePlayerList(room_info.Players);
                        session.Socket.SendPacket(new GetDataPackagePacket());
                        break;
                    }
                case ArchipelagoPacketType.ConnectionRefused:
                    {
                        var p = packet as ConnectionRefusedPacket;
                        foreach (var err in p.Errors)
                        {
                            Debug.LogError(err.ToString());
                        }
                        break;
                    }
                case ArchipelagoPacketType.Connected:
                    {
                        connected_data = packet as ConnectedPacket;
                        updatePlayerList(connected_data.Players);
                        break;
                    }
                case ArchipelagoPacketType.ReceivedItems:
                    {
                        if (state == State.Menu)
                        {
                            // Ignore, we will do a sync when the game starts
                            break;
                        }

                        var p = packet as ReceivedItemsPacket;
                        //ErrorMessage.AddError("next_item_index: " + next_item_index.ToString() + ", Index: " + p.Index.ToString() + ", items: " + p.Items.Count.ToString());
                        Debug.Log("next_item_index: " + next_item_index.ToString() + ", Index: " + p.Index.ToString() + ", items: " + p.Items.Count.ToString());

                        if (state == State.AwaitingSync)
                        {
                            if (next_item_index > 0)
                            {
                                // Make sure to not unlock stuff we've already unlocked
                                p.Items.RemoveRange(0, next_item_index);
                            }
                            state = State.InGame;
                        }
                        else if (next_item_index < p.Index)
                        {
                            ErrorMessage.AddError("Out of sync with server, resynchronizing");
                            // Resync, we're out of sync!
                            var sync_packet = new SyncPacket();
                            session.Socket.SendPacket(sync_packet);
                            state = State.AwaitingSync;
                            break;
                        }

                        foreach (var item in p.Items)
                        {
                            unlock_queue.Add(ITEM_CODE_TO_TECHTYPE[item.Item]);
                        }

                        //next_item_index += p.Items.Count;

                        break;
                    }
                case ArchipelagoPacketType.LocationInfo:
                    {
                        // This should contain all our checks
                        location_infos = packet as LocationInfoPacket;
                        break;
                    }
                case ArchipelagoPacketType.RoomUpdate:
                    {
                        var p = packet as RoomUpdatePacket;
                        // Hint points? Dont care
                        break;
                    }
                case ArchipelagoPacketType.Print:
                    {
                        var p = packet as PrintPacket;
                        message_queue.Add(p.Text);
                        //ErrorMessage.AddMessage(p.Text);
                        break;
                    }
                case ArchipelagoPacketType.PrintJSON:
                    {
                        var p = packet as PrintJsonPacket;
                        string text = "";
                        foreach (var messagePart in p.Data)
                        {
                            switch (messagePart.Type)
                            {
                                case "player_id":
                                    text += int.TryParse(messagePart.Text, out var playerSlot)
                                        ? session.Players.GetPlayerAliasAndName(playerSlot) ?? $"Slot: {playerSlot}"
                                        : messagePart.Text;
                                    break;
                                case "item_id":
                                    text += int.TryParse(messagePart.Text, out var itemId)
                                        ? session.Items.GetItemName(itemId) ?? $"Item: {itemId}"
                                        : messagePart.Text;
                                    break;
                                case "location_id":
                                    text += int.TryParse(messagePart.Text, out var locationId)
                                        ? session.Locations.GetLocationNameFromId(locationId) ?? $"Location: {locationId}"
                                        : messagePart.Text;
                                    break;
                                default:
                                    text += messagePart.Text;
                                    break;
                            }
                        }
                        message_queue.Add(text);
                        //ErrorMessage.AddMessage(text);
                        break;
                    }
                case ArchipelagoPacketType.DataPackage:
                    {
                        data_package = packet as DataPackagePacket;

                        var connect_packet = new ConnectPacket();

                        connect_packet.Game = "Subnautica";
                        connect_packet.Name = player_name;
                        connect_packet.Uuid = Convert.ToString(player_name.GetHashCode(), 16);
                        connect_packet.Version = new Version(AP_VERSION[0], AP_VERSION[1], AP_VERSION[2]);
                        connect_packet.Tags = new List<string> { "AP" };
                        connect_packet.Password = password;

                        ApState.session.Socket.SendPacket(connect_packet);
                        break;
                    }
            }
        }

        public static void updatePlayerList(List<MultiClient.Net.Models.NetworkPlayer> players)
        {
            player_names_by_id = new Dictionary<int, string>
            {
                { 0, "Archipelago" }
            };

            foreach (var player in players)
            {
                player_names_by_id[player.Slot] = player.Name;
            }
        }

        public static bool checkLocation(Vector3 position)
        {
            int closest_id = -1;
            float closest_dist = 100000.0f;
            foreach (var location in LOCATIONS)
            {
                var dist = Vector3.Distance(location.position, position);
                if (dist < closest_dist && dist < 1.0f) // More than 10m, ignore. Can't have errors that big... can we?
                {
                    closest_dist = dist;
                    closest_id = location.id;
                }
            }

            if (closest_id != -1)
            {
                var location_packet = new LocationChecksPacket();
                location_packet.Locations = new List<int> { closest_id };
                ApState.session.Socket.SendPacket(location_packet);
                return true;
            }

            return false;
        }

        public static void unlock(TechType techType)
        {
            if (PDAScanner.IsFragment(techType))
            {
                PDAScanner.EntryData entryData = PDAScanner.GetEntryData(techType);

                PDAScanner.Entry entry;
                if (!PDAScanner.GetPartialEntryByKey(techType, out entry))
                {
                    MethodInfo methodAdd = typeof(PDAScanner).GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(TechType), typeof(int) }, null);
                    entry = (PDAScanner.Entry)methodAdd.Invoke(null, new object[] { techType, 0 });
                }

                if (entry != null)
                {
                    entry.unlocked++;

                    if (entry.unlocked >= entryData.totalFragments)
                    {
                        List<PDAScanner.Entry> partial = (List<PDAScanner.Entry>)(typeof(PDAScanner).GetField("partial", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                        HashSet<TechType> complete = (HashSet<TechType>)(typeof(PDAScanner).GetField("complete", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
                        partial.Remove(entry);
                        complete.Add(entry.techType);

                        MethodInfo methodNotifyRemove = typeof(PDAScanner).GetMethod("NotifyRemove", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(PDAScanner.Entry) }, null);
                        methodNotifyRemove.Invoke(null, new object[] { entry });

                        MethodInfo methodUnlock = typeof(PDAScanner).GetMethod("Unlock", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(PDAScanner.EntryData), typeof(bool), typeof(bool), typeof(bool) }, null);
                        methodUnlock.Invoke(null, new object[] { entryData, true, false, true });
                    }
                    else
                    {
                        int totalFragments = entryData.totalFragments;
                        if (totalFragments > 1)
                        {
                            float num2 = (float)entry.unlocked / (float)totalFragments;
                            float arg = (float)Mathf.RoundToInt(num2 * 100f);
                            ErrorMessage.AddError(Language.main.GetFormat<string, float, int, int>("ScannerInstanceScanned", Language.main.Get(entry.techType.AsString(false)), arg, entry.unlocked, totalFragments));
                        }

                        MethodInfo methodNotifyProgress = typeof(PDAScanner).GetMethod("NotifyProgress", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(PDAScanner.Entry) }, null);
                        methodNotifyProgress.Invoke(null, new object[] { entry });
                    }
                }
            }
            else
            {
                // Blueprint
                KnownTech.Add(techType, true);
            }
        }
        */
    }
}
