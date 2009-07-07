using System;
using System.Collections.Generic;
//using System.Linq;
using System.Threading;
using System.Text;
using OpenMetaverse;

// CONSTANTS

namespace ALIVE

{
    /// <summary>
    /// Docs go here
    /// </summary>
    public enum primType {
        Unknown,
        Box,
        Cylinder,
        Prism,
        Sphere,
        Torus,
        Tube,
        Ring,
        Sculpt
    };

    public class Prim
    {
        public ulong ID;
        public uint LocalID;
        public float X;
        public float Y;
        public primType pType;
        public bool movable;
        public string color;
        public string name;
        public string description;
        public float angle;
        public float sizeX, sizeY, sizeZ;
        public float colorR, colorG, colorB;

        public string primTypeToString(primType t)
        {
            string[] names = { "Unknown", "Box", "Cylinder", "Prism", "Sphere", "Torus",
                             "Tube", "Ring", "Sculpt"};
            return names[(int)t];
        }

        // Constructor
        public Prim (Primitive p)
        {
            ID = p.ID.GetULong();
            LocalID = p.LocalID;

            X = p.Position.X;
            Y = p.Position.Y;
            
            sizeX = p.Scale.X;
            sizeY = p.Scale.Y;
            sizeZ = p.Scale.Z;

            color = color2String(
                p.Textures.DefaultTexture.RGBA.R, 
                p.Textures.DefaultTexture.RGBA.G, 
                p.Textures.DefaultTexture.RGBA.B);

            movable = (p.Flags & PrimFlags.ObjectMove) != 0;
            pType = (primType) p.Type;

            name = "";
            description = "";
            if (p.Properties != null) {
                if (p.Properties.Name != null)
                    name = p.Properties.Name;
                if (p.Properties.Description != null)
                    description = p.Properties.Description;
            }

            angle = Bot.ZrotFromQuaternion(p.Rotation);
        }

        // simple mapping of basic RGB values to colors
        // This can be expanded and made more 'fuzzy'
        private String color2String(float r, float g, float b)
        {
            if (r == 0 && g == 0 && b == 0) return "black";
            if (r == 1 && g == 1 && b == 1) return "white";
            if (r == 1 && g == 0 && b == 0) return "red";
            if (r == 0 && g == 1 && b == 0) return "green";
            if (r == 0 && g == 0 && b == 1) return "blue";
            if (r == 1 && g == 1 && b == 0) return "yellow";
            if (r == 0 && g == 1 && b == 1) return "aqua";
            if (r == 1 && g == 0 && b == 1) return "purple";
            return "undefined";
        }

        public string toString()
        {
            // ALIVE objects use only LocalID, leave out global UUID
            // ID.ToString()
            return LocalID.ToString() + " <" + X.ToString("0.0") + "," + Y.ToString("0.0")
                + "> " + pType + " " +
                angle.ToString("0.0") +
                " [" + sizeX.ToString("0.0") + "," + sizeY.ToString("0.0") + "," + sizeZ.ToString("0.0") + "] " +
                color + " " + movable + " " + name + " " + description;
        }

    };

    public class Bot
    {
        const string ALIVE_SERVER = "http://osmort.lti.cs.cmu.edu:9000";
        const string SECONDLIFE_SERVER = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
        const string WORLD_MASTER_NAME = "World Master";
        const int SEARCH_RADIUS = 10;

        private Dictionary<String, UUID> AvatarNames;
        private UUID WorldMasterUUID = new UUID(0L);

        public string FirstName;
        public string LastName;
        public string Password;
        private Boolean loggedIn = false;

        private GridClient client;

        // constructor
        public Bot(string fn, string ln, string pw) {
            FirstName = fn;
            LastName = ln;
            Password = pw;

            AvatarNames = new Dictionary<String, UUID>();
            client = new GridClient();

            client.Network.OnConnected +=new NetworkManager.ConnectedCallback(Network_OnConnected);
            client.Self.OnChat += new AgentManager.ChatCallback(Self_OnChat);
            client.Self.OnInstantMessage += new AgentManager.InstantMessageCallback(Self_OnInstantMessage);

            client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Self_OnNewAvatar);
            //client.Self.OnMeanCollision += new AgentManager.MeanCollisionCallback(Self_OnMeanCollision);

            // Register callback to catch Object properties events
            client.Objects.OnObjectProperties += new ObjectManager.ObjectPropertiesCallback(Objects_OnObjectProperties);
        }

        // methods

        public bool Login()
        {
            Boolean LoginSuccess;

            LoginParams loginParams = new LoginParams();
            loginParams = client.Network.DefaultLoginParams(FirstName, LastName, Password, "ALIVE", "Bot");
            //loginParams.Start = "last"; // specify start location
            loginParams.URI = ALIVE_SERVER;
            LoginSuccess = client.Network.Login(loginParams);

            //client.Network.CurrentSim.ObjectsAvatars.ForEach();
            //client.Network.CurrentSim.ObjectsPrimitives.ForEach();

            if (LoginSuccess)
            {
                // update location, avatars, objects nearby here?
                loggedIn = true;
            }
            else
            {
                Console.WriteLine("Din't work! " + client.Network.LoginMessage);
                return false;
            }

            // This is what seems to have solved the cloud avatar problem
            client.Appearance.SetPreviousAppearance(false);

            //client.Network.CurrentSim.ObjectsAvatars.ForEach(OpenMetaverse.Avatar.AvatarProperties.Equals);

            return true;
        }

        public void Logout() 
        {
            if (loggedIn) client.Network.Logout();
        }

        // Movement commands

        public void TurnTo (int x, int y) 
        {
            Vector3 position = new Vector3(x, y, 0);

            client.Self.Movement.TurnToward(position);
        }

        public void TurnLeft(long degrees) 
        {
            float angle = (float)Math.PI * (float) degrees / -360f;

            Quaternion rot = new Quaternion(0, 0, (float)Math.Cos(angle/2), (float)Math.Sin(angle/2));

            // I don't know why but doing it twice makes it work            
            Quaternion q = client.Self.Movement.BodyRotation * rot * rot;

            client.Self.Movement.BodyRotation = q;
            client.Self.Movement.HeadRotation = q;
            client.Self.Movement.SendUpdate(true);
        }

        public void TurnRight(long degrees)
        {
            // Doing the reverse angle computation results in all
            // kinds of hurt - this actually works better
            // (maybe rotation by multiplying Quaternions only
            // works in one direction???  Maybe some min/max issue?)
            TurnLeft(-degrees);
        }

        public void GoForward(int meters) {
            client.Self.Movement.AtPos = true;
            client.Self.Movement.SendUpdate(true);
            // 3 meters per second, so 333 msec per meter
            Thread.Sleep(333 * meters);
            client.Self.Movement.AtPos = false;
            client.Self.Movement.SendUpdate(true);
        }

        public void GoBackward(int meters) {
            client.Self.Movement.AtNeg = true;
            client.Self.Movement.SendUpdate(true);
            // 3 meters per second, so 333 msec per meter
            Thread.Sleep(333 * meters);
            client.Self.Movement.AtNeg = false;
            client.Self.Movement.SendUpdate(true);
        }

        public bool GoTo(int x, int y)
        {
            Vector3 pos = client.Self.SimPosition;
            float Z = 0;
            //int tries = 0;
            //bool success = false;
            //while ( !success) {
            //success = 
            //    client.Terrain.TerrainHeightAtPoint(client.Network.CurrentSim.Handle, x, y, out Z);
            //Thread.Sleep(1000);
            //Console.Out.WriteLine("try: " + tries++ + " " + Z);
            //}

            //if (!success)
                Z = client.Self.SimPosition.Z;

            float distance = Vector3.Distance(pos, new Vector3(x, y, Z));
            client.Self.AutoPilotLocal(x, y, Z);

            // Guess that avatar travels at 3m/sec
            // wait for them to get there
            Thread.Sleep(Convert.ToInt32(333 *distance) + 500);
            pos = client.Self.SimPosition;

            Console.Out.WriteLine("Completed autopilot at: " + pos.X + "," + pos.Y);
            
            // Need to make these fuzzy
            float fuzz = 0.8f;
            if (Math.Abs(pos.X - x) < fuzz &&  Math.Abs(pos.Y - y) < fuzz)
                return true;
            else
                return false;
        }

        // Avatar properties

        // <summary>
        // Return the coordinates of the avatar within a 256 by 256 meter
        // Simulator
        // </summary>
        public void MyCoordinates(out float x, out float y)
        {
            Vector3 pos = client.Self.SimPosition;
            x = pos.X;
            y = pos.Y;
        }


        // Given a Quaternion, return the rotation around the Z
        // (vertical) axis in degrees
        public static float ZrotFromQuaternion(Quaternion q) {
            // Borrowed code works MUCH better than GetEulerAngles()
            // or other GetAxisAngle()
            Vector3 v3 = Vector3.Transform(Vector3.UnitX, Matrix4.CreateFromQuaternion(q));
            float newangle = (float)Math.Atan2(v3.Y, v3.X);
            return newangle * 180 / (float)Math.PI;
        }

        // <summary>
        // Return rotation of bot avatar in degrees
        // (due East is zero, results are from -180 to +180)
        // </summary>
        public float MyOrientation()
        {
            return ZrotFromQuaternion(client.Self.RelativeRotation);
        }

        // Object commands

        public List<Prim> ObjectsAround(float radius)
        {
            Vector3 location = client.Self.SimPosition;
            
            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                });

            
            // *** request properties of (only) these objects ***
            bool complete = RequestObjectProperties(prims, 500, true);

            //Console.Out.WriteLine("Properties completed: " + complete);

            List<Prim> returnPrims = new List<Prim>();
            foreach (Primitive p in prims) {
                // convert OpenMetaverse Primitive to ALIVE Prim
                returnPrims.Add(new Prim(p));
            }
            return returnPrims;
        }

        // Override
        public List<Prim> ObjectsAround()
        {
            return ObjectsAround(SEARCH_RADIUS);
        }


        public void DropObject(uint item) 
        {
            client.Objects.DropObject(client.Network.CurrentSim, item);
        }


        // We will use "attach" to pick up an object by
        // carrying it by hand, i.e. attach to left or right hand
        public void PickupObject(uint item) 
        {
            client.Objects.AttachObject(client.Network.CurrentSim, item, AttachmentPoint.LeftHand, Quaternion.Identity);
        }


        // CHAT COMMANDS

        // World Master message
        public string GetMessage () 
        {
            string temp = imb;
            imb = "";
            return temp; 
        }
        
        public void SayMessage (string message) 
        {
            ulong id = WorldMasterUUID.GetULong();
            // Find out UUID of World master by avatar name lookup
            if (WorldMasterUUID.GetULong() != 0L)
                client.Self.InstantMessage(WorldMasterUUID, message);
        }

        // Chat message
        public string GetChat () 
        {
            string temp = cb;
            cb = "";
            return temp;
        }

        public void SayChat(string message) 
        {
            // Chat on channel 0 = nearby 20 m
            client.Self.Chat(message, 0, ChatType.Normal);
        }


        // CALLBACKS

        // Request Object properties
        // if synchronous == false, the msPerRequest is not needed
        // and it returns immediately, letting event handlers deal
        // with the results
        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest, bool synchronous)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }

            client.Objects.SelectObjects(client.Network.CurrentSim, localids);

            if (synchronous)
                return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
            else
                return true;
        }

        // Callback for ObjectProperties request received

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        void Objects_OnObjectProperties(Simulator simulator, Primitive.ObjectProperties properties)
        {
            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(properties.ObjectID, out prim))
                {
                    prim.Properties = properties;
                    PrimsWaiting.Remove(properties.ObjectID);
                    //Console.Out.WriteLine("Name: " + properties.Name + " Desc: " + properties.Description);
                }
                else
                {
                    // Called for an object we didn't ask for
                }

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }

        // Attempt to wear the inventory item named by the argument itemName
        // Does not report error on failure
        void wearNamedItem(string itemName)
        {
            //initialize our list to store the folder contents
            UUID inventoryItems;

            //make a string array to put our folder names in.
            String[] SearchFolders = { "" };

            //Next we grab a full copy of the entire inventory and get it stored into the Inventory Manager
            client.Inventory.RequestFolderContents(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);

            //Next we want to step through the directory structure until we get to the item.
            SearchFolders[0] = "Objects";

            //Now we can grab the details of that folder and store it to our list.
            inventoryItems = client.Inventory.FindObjectByPath(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, SearchFolders[0], 1000);

            //now that we have the details of the objects folder, we need to grab the details of our torch.
            SearchFolders[0] = itemName;
            inventoryItems = client.Inventory.FindObjectByPath(inventoryItems, client.Self.AgentID, SearchFolders[0], 1000);

            InventoryItem myitem;

            // Convert the LLUUID to an inventory item
            myitem = client.Inventory.FetchItem(inventoryItems, client.Self.AgentID, 1000);

            //finally we attach the object to it's default position
            try
            {
                // Catch any errors that may occur (not having the "Torch!" item in your inventory for example)
                client.Appearance.Attach(myitem as InventoryItem, AttachmentPoint.Default);
            }
            catch
            {
                // Put any code that handles any errors :)
                System.Console.WriteLine("Error attaching " + itemName);
            }
        }
        
/////// HANDLERS
        public void Network_OnConnected(Object sender)
        {
            System.Console.WriteLine("Successfully connected");
        }

        public void Self_OnInstantMessage(InstantMessage im, Simulator sim)
        {
            System.Console.WriteLine("Instant Message: " + im.Message);
            if (im.Message != "typing")
            imb = imb + im.FromAgentName + ": " + im.Message + "\r\n";
        }

        // Keep track of avatar names to find World Master UUID
        
        public void Self_OnNewAvatar(Simulator sim, Avatar av, ulong regionHandle, ushort timeDilation)
        {
            string avatarName = av.FirstName + " " + av.LastName;

            if (AvatarNames.ContainsKey(avatarName)) 
                return;
            AvatarNames.Add(avatarName, av.ID);
            if (avatarName == WORLD_MASTER_NAME)
                WorldMasterUUID = av.ID;
        }

        // Chat buffers
        //
        // cb - chat buffer
        // imb - instant message buffer
        private static string cb;
        private static string imb;

        public void Self_OnChat(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourceType, string fromName, UUID id, UUID ownerid, Vector3 position)
        {
            System.Console.WriteLine("Chat message: " + message);
            //if (ChatType.OwnerSay == type && sourceType == ChatSourceType.Object)
            //// scripted object in world talking; we assume object scanner
            //{
            //    message = message.Replace("\n", "\r\n");
            //    textBoxUpdate(objectsBox, message);
            //    return;
            //}
            //else 
            if (audible != ChatAudibleLevel.Fully || type == ChatType.StartTyping || type == ChatType.StopTyping)
                return;
            if (sourceType == ChatSourceType.Object) 
                return;

            cb = cb + fromName + ": " + message + "\r\n";
        }


    } // Avatar

} // ALIVE
