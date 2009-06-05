using System;
using System.Collections.Generic;
//using System.Linq;
using System.Threading;
using System.Text;
using OpenMetaverse;

namespace ALIVE
{
    public class Avatar
    {
        public string FirstName;
        public string LastName;
        public string Password;
        const string ALIVE_SERVER = "http://osmort.lti.cs.cmu.edu:9000";
        const string SECONDLIFE_SERVER = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
        private GridClient client;

        // constructor
        public Avatar(string fn, string ln, string pw) {
            FirstName = fn;
            LastName = ln;
            Password = pw;

            client = new GridClient();

            client.Network.OnConnected +=new NetworkManager.ConnectedCallback(Network_OnConnected);
            //client.Self.OnMeanCollision += new AgentManager.MeanCollisionCallback(Self_OnMeanCollision);
            client.Self.OnChat += new AgentManager.ChatCallback(Self_OnChat);
            //client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Self_OnNewAvatar);

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
                
            }
            else
            {
                Console.WriteLine("Din't work! " + client.Network.LoginMessage);
                return false;
            }

            // This is what seems to have solved the cloud avatar problem
            client.Appearance.SetPreviousAppearance(false);

            return true;
        }

        public void Logout() 
        {
            client.Network.Logout();
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

            Console.Out.WriteLine("ANGLE: " + angle);
            //Vector3 vec = new Vector3(0, 0, angle);
            //Quaternion rot = new Quaternion(x);
            Quaternion rot = new Quaternion(0, 0, (float)Math.Cos(angle/2), (float)Math.Sin(angle/2));
            
            Console.Out.WriteLine("ROT: " + rot.ToString());
            rot.Normalize();
            Console.Out.WriteLine("ROT: " + rot.ToString());
            
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
            // 6 meters per second, so 166 msec per meter
            Thread.Sleep(166 * meters);
            client.Self.Movement.AtNeg = false;
            client.Self.Movement.SendUpdate(true);
        }

        public bool GoTo(int x, int y)
        {
            Vector3 pos = client.Self.SimPosition;
            client.Self.AutoPilotLocal(x, y, pos.Z);

            pos = client.Self.SimPosition;

            if ((int)pos.X == x && (int)pos.Y == y)
                return true;
            else
                return false;
        }

        // Avatar properties

        public void MyCoordinates(out int x, out int y)
        {
            Vector3 pos = client.Self.SimPosition;
            x = (int)pos.X;
            y = (int)pos.Y;
        }

        public float MyOrientation()
        {
            // This may be BROKEN until we determine
            // what is going on with GetAxisAngle and GetEulerAngles
            // since they return bizarro values

            Vector3 axis;
            float angle;
            client.Self.SimRotation.GetAxisAngle(out axis, out angle);
            // 2*pi radians = 360 degrees
            return angle * 180 / (float)Math.PI;
        }

        // Object commands

        // Primitives have these useful properties:
        // location:    Vector3  prim.Position
        // size:        Vector3  prim.Scale
        // name:        string   prim.Properties.Name
        // description: string   prim.Properties.Description
        // type:        PrimType prim.Type
        //Unknown = 0,
        //Box = 1,
        //Cylinder = 2,
        //Prism = 3,
        //Sphere = 4,
        //Torus = 5,
        //Tube = 6,
        //Ring = 7,
        //Sculpt = 8,
        public List<Primitive> ObjectsAround(float radius)
        {
            Vector3 location = client.Self.SimPosition;
            
            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                });

            
            // *** request properties of (only) these objects ***
            bool complete = RequestObjectProperties(prims, 250, true);

            // convert Primitives to ALIVE objects here
            
            //string objs = objectsBox.Text + prettyLocation(prim.Position) + "  " + 
            //    prettySize(prim.Scale) + " " +
            //    properties.Name + " " + properties.Description + "\r\n";

            return prims;
        }

        public void DropObject() {}

        public void PickupObject(UUID item) {}

        // CHAT COMMANDS HERE

        // World Master message
        public string GetMessage () { return "hello"; }
        public void SayMessage () { }

        // Chat message
        public string GetChat () 
        {
            string temp = cb;
            cb = "";
            return cb;
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
        //Dictionary<UUID, Primitive> PrimsFound = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        void Objects_OnObjectProperties(Simulator simulator, Primitive.ObjectProperties properties)
        {

            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(properties.ObjectID, out prim))
                {
                    prim.Properties = properties;


                    //PrimsFound.Add(prim.ID, prim);
                }
                PrimsWaiting.Remove(properties.ObjectID);

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

        private static string cb;
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

            cb = cb + "\r\n" + fromName + ": " + message;
        }


    } // Avatar

} // ALIVE
