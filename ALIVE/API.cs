using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using OpenMetaverse;

namespace ALIVE

{
    ///<returns></returns>
    public enum primType {
        ///<summary>Unknown primitive type</summary>
        Unknown,
        ///<summary>Box primitive type</summary> 
        Box,
        ///<summary>Cylinder primitive type</summary>
        Cylinder,
        ///<summary>Prism primitive type</summary>
        Prism,
        ///<summary>Sphere primitive type</summary>
        Sphere,
        ///<summary>Torus primitive type</summary>
        Torus,
        ///<summary>Tube primitive type</summary>
        Tube,
        ///<summary>Ring primitive type</summary>
        Ring,
        ///<summary>Sculpted primitive type</summary>
        Sculpt
    };
    

    /// <summary>The most basic type of ALIVE object, akin to a Second Life Primitive</summary>
    public class AliveObject
    {
        ///<summary>64 bit Global ID (unique across the virtual world)</summary>
        private ulong ID;
        ///<summary>32 bit Local ID (unique within the current region)
        /// Although public, this is for internal use only.</summary>
        public uint LocalID;
        ///<summary>X coordinate within current region</summary>
        public float X;
        ///<summary>Y coordinate within current region</summary>
        public float Y;
        ///<summary>Primitive type (see primType)</summary>
        public string shape;
        ///<summary>Can the Prim be picked up or moved?</summary>
        public bool movable;
        ///<summary>String representing one of the colors: red, blue, green, yellow, aqua, purple, black, white
        ///Other colors are not represented here and appear as "unknown"</summary>
        public string color;
        ///<summary>Name of the AliveObject as it appears in-world, for example $ball3</summary>
        public string name;
        ///<summary>Description of the Primitive as it appears in-world.  
        /// Currently unused,
        /// this can be overloaded to store metadata about the object</summary>
        public string description;
        ///<summary>Rotation of the primary face of the Prim around the vertical axis 
        ///         in degrees, measured counter-clockwise from due East.</summary>
        public float angle;
        ///<summary>Size of the Primitive in 3 dimensions</summary>
        public float width, depth, height;
        //public float colorR, colorG, colorB;

        // Constructor
        /// <summary>The most basic type of ALIVE object, akin to a Second Life Primitive</summary>
        public AliveObject (Primitive p)
        {
            ID = p.ID.GetULong();
            LocalID = p.LocalID;

            X = p.Position.X;
            Y = p.Position.Y;
            
            width = p.Scale.X;
            depth = p.Scale.Y;
            height = p.Scale.Z;

            color = SmartDog.color2String(
                p.Textures.DefaultTexture.RGBA.R, 
                p.Textures.DefaultTexture.RGBA.G, 
                p.Textures.DefaultTexture.RGBA.B);

            movable = (p.Flags & PrimFlags.ObjectMove) != 0;
            shape = p.Type.ToString();

            name = "";
            description = "";
            if (p.Properties != null) {
                if (p.Properties.Name != null)
                    name = p.Properties.Name;
                if (p.Properties.Description != null)
                    description = p.Properties.Description;
            }

            angle = SmartDog.ZrotFromQuaternion(p.Rotation);
        }



        ///<summary>Returns a printable description of Prim attributes</summary>
        public string toString()
        {
            // ALIVE objects use only LocalID, leave out global UUID
            // ID.ToString()
            return LocalID.ToString() + " <" + X.ToString("0.0") + "," + Y.ToString("0.0")
                + "> " + shape + " " +
                angle.ToString("0.0") +
                " [" + width.ToString("0.0") + "," + depth.ToString("0.0") + "," + height.ToString("0.0") + "] " +
                color + " " + movable + " " + name + " " + description;
        }
    }; // public class AliveObject


    ///<summary>Object which represents an avatar in ALIVE/OpenMetaverse/SecondLife</summary>
    public class SmartDog
    {
        /// naughty 'globals'
        //const string ALIVE_SERVER = "http://osmort.lti.cs.cmu.edu:9000";
        const string ALIVE_SERVER = "http://ohio.pc.cs.cmu.edu:9000";
        const string SECONDLIFE_SERVER = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
        const string WORLD_MASTER_NAME = "World Master";
        const int SEARCH_RADIUS = 10;
        const int walkSpeed = 320; // msec per meter

        private Dictionary<String, UUID> AvatarNames;
        private UUID WorldMasterUUID = new UUID(0L);
        private Boolean logging = true;

        private string FirstName;
        private string LastName;
        private string Password;
        private Boolean loggedIn = false;

        private GridClient client;

        ///<summary>Construct a new Bot</summary>
        ///<param name='fn'>first name</param>
        ///<param name='ln'>last name</param>
        ///<param name='pw'>password</param>>
        public SmartDog(string fn, string ln, string pw) {
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

        private void logThis(String args) {
            if (logging)
            {
                System.Diagnostics.StackFrame stackFrame
                    = new System.Diagnostics.StackFrame(1);

                System.Reflection.MethodBase methodBase =
                    stackFrame.GetMethod();

                string methodName = methodBase.Name;
                string message = DateTime.Now.ToString("[HH:mm:ss:fff]") + methodName + " (" + args + ")";

                Console.WriteLine(message);
                SayMessage(message);
            }
        }

        ///<summary>Attempt to log the avatar into the default region</summary>
        public bool Login()
        {
            Boolean LoginSuccess;

            LoginParams loginParams = new LoginParams();
            loginParams = client.Network.DefaultLoginParams(FirstName, LastName, Password, "ALIVE", "Bot");
            loginParams.Start = "home"; // specify start location.  We set avatars homes at 128,128
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

            logThis("");
            return true;
        }

        ///<summary>Log the avatar out</summary>
        public bool Logout() 
        {
            logThis("");
            if (loggedIn) client.Network.Logout();

            return true;
        }

        // Movement commands

        ///<summary>Rotate the avatar to face a specified location</summary>
        ///<param name='x'>X coordinate</param>
        ///<param name='y'>Y coordinate</param>
        public void TurnTo (int x, int y) 
        {
            logThis(x + "," + y );

            Vector3 position = new Vector3(x, y, 0);

            client.Self.Movement.TurnToward(position);
        }

        ///<summary>Rotate the avatar counter-clockwise</summary>
        ///<param name='degrees'>degrees to rotate</param>
        public bool TurnLeft(long degrees) 
        {
            logThis(degrees.ToString());

            float angle = (float)Math.PI * (float) degrees / -360f;

            Quaternion rot = new Quaternion(0, 0, (float)Math.Cos(angle/2), (float)Math.Sin(angle/2));

            // I don't know why but doing it twice makes it work            
            Quaternion q = client.Self.Movement.BodyRotation * rot * rot;

            client.Self.Movement.BodyRotation = q;
            client.Self.Movement.HeadRotation = q;
            client.Self.Movement.SendUpdate(true);

            return true;
        }

        ///<summary>Rotate the avatar clockwise</summary>
        ///<param name='degrees'>degrees to rotate</param>
        public bool TurnRight(long degrees)
        {
            logThis(degrees.ToString());

            float angle = (float)Math.PI * (float)degrees / 360f;

            Quaternion rot = new Quaternion(0, 0, (float)Math.Cos(angle / 2), (float)Math.Sin(angle / 2));

            // I don't know why but doing it twice makes it work            
            Quaternion q = client.Self.Movement.BodyRotation * rot * rot;

            client.Self.Movement.BodyRotation = q;
            client.Self.Movement.HeadRotation = q;
            client.Self.Movement.SendUpdate(true);

            return true;
        }

        ///<summary>Attempt to walk the avatar forward in a straight line.  Obstacles may prevent this from completing as expected</summary>
        ///<param name='meters'>Distance to walk in meters</param>
        public bool GoForward(int meters) {
            logThis(meters.ToString());

            // Find current location, orientation and compute target location
            float currentX, currentY, targetX, targetY;
            float x, y;
            double angle;
            
            // Get position
            Vector3 pos = client.Self.SimPosition;
            x = pos.X;
            y = pos.Y;
            
            currentX = x;
            currentY = y;

            // Get orientation
            float a = ZrotFromQuaternion(client.Self.RelativeRotation);
            angle = 90 - a;
            if (angle < 0) angle = 360 + angle;
            
            targetX = x + (float) (meters * Math.Sin(angle / 180 * Math.PI));
            targetY = y + (float) (meters * Math.Cos(angle / 180 * Math.PI));

            client.Self.Movement.AtPos = true;
            client.Self.Movement.SendUpdate(true);
            
            Thread.Sleep(walkSpeed * meters);
            client.Self.Movement.AtPos = false;
            client.Self.Movement.SendUpdate(true);

            // Get position
            pos = client.Self.SimPosition;
            x = pos.X;
            y = pos.Y;

            // Compare to expected destination
            float fuzz = 1.0f;

            if ((Math.Abs(x - targetX) < fuzz) && (Math.Abs(y - targetY) < fuzz))
                return true;
            else
            {
                Console.WriteLine("Off by " + (x - targetX) + " horizontal, " +
                    (y - targetY) + " vertical");
                return false;
            }
        }

        ///<summary>Attempt to walk the avatar backwards in a straight line.  Obstacles may prevent this from completing as expected</summary>
        ///<param name='meters'>Distance to walk in meters</param>
        public bool GoBackward(int meters) {
            logThis(meters.ToString());

            // Find current location, orientation and compute target location
            float currentX, currentY, targetX, targetY;
            float x, y;
            double angle;

            // Get position
            Vector3 pos = client.Self.SimPosition;
            x = pos.X;
            y = pos.Y;

            currentX = x;
            currentY = y;

            // Get orientation
            float a = ZrotFromQuaternion(client.Self.RelativeRotation);
            angle = 90 - a;
            if (angle < 0) angle = 360 + angle;

            targetX = x - (float)(meters * Math.Sin(angle / 180 * Math.PI));
            targetY = y - (float)(meters * Math.Cos(angle / 180 * Math.PI));

            client.Self.Movement.AtNeg = true;
            client.Self.Movement.SendUpdate(true);
            
            Thread.Sleep(walkSpeed * meters);
            client.Self.Movement.AtNeg = false;
            client.Self.Movement.SendUpdate(true);

            // Get position
            pos = client.Self.SimPosition;
            x = pos.X;
            y = pos.Y;

            // Compare to expected destination
            float fuzz = 1.0f;

            if ((Math.Abs(x - targetX) < fuzz) && (Math.Abs(y - targetY) < fuzz))
                return true;
            else
            {
                Console.WriteLine("Off by " + (x - targetX) + " horizontal, " +
                    (y - targetY) + " vertical");
                return false;
            }
        }

        ///<summary>Go to the specified location.</summary>
        ///<remarks>This is not very reliable, in Second Life, or in ALIVE, and can 
        ///result in the avatar getting stuck.  Use with caution.  This routine returns
        ///after the time taken to travel this distance,
        ///assuming a travel speed of 3 meters per second.</remarks>
        ///
        ///<param name="x">X coordinate of location to attempt to travel to</param>
        ///<param name="y">Y coordinate of location to attempt to travel to</param>
        ///<returns>True or false depending on whether the location was reached (within a margin of error of 0.8 meters - from experimental data)</returns>
        public bool GoTo(int x, int y)
        {
            logThis(x + "," + y);

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
            Thread.Sleep(Convert.ToInt32(320 *distance) + 500);
            pos = client.Self.SimPosition;

            Console.Out.WriteLine("Completed autopilot at: " + pos.X + "," + pos.Y);
            
            // Need to make these fuzzy; final location not exact
            float fuzz = 0.8f;
            if ((Math.Abs(pos.X - x) < fuzz) &&  (Math.Abs(pos.Y - y) < fuzz))
                return true;
            else
                return false;
        }

        // Avatar properties

        /// <summary>
        /// Return the coordinates of the avatar (in meters) within a 256 by 256 meter
        /// Simulator region as floating point values.  X is due East, and Y is due North.
        /// </summary>
        /// <param name="x">X coordinate of avatar</param>
        /// <param name="y">Y coordinate of avatar</param>
        public void Coordinates(out float x, out float y)
        {
            logThis("");

            Vector3 pos = client.Self.SimPosition;
            x = pos.X;
            y = pos.Y;
        }


        /// UTILITY FUNCTIONS

        // simple mapping of basic RGB values to colors
        // This can be expanded and made more 'fuzzy'
        public static String color2String(float r, float g, float b)
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

        /// <summary>Given a Quaternion, return the rotation around the Z
        /// (vertical) axis in degrees.</summary><remarks>Angles are measured counterclockwise
        /// from due East</remarks>
        /// <returns>floating point angle in degrees</returns>
        public static float ZrotFromQuaternion(Quaternion q) {
            // Borrowed code works MUCH better than GetEulerAngles()
            // or other GetAxisAngle()
            Vector3 v3 = Vector3.Transform(Vector3.UnitX, Matrix4.CreateFromQuaternion(q));
            float newangle = (float)Math.Atan2(v3.Y, v3.X);
            return newangle * 180 / (float)Math.PI;
        }

        /// <summary>
        /// Return rotation of bot avatar in degrees clockwise from Due North</summary>
        /// <remarks>(due North is zero, results are from 0 to 360)
        /// </remarks>
        public float Orientation()
        {
            logThis("");

            float angle = ZrotFromQuaternion(client.Self.RelativeRotation);
            float retval = 90 - angle;
            if (retval < 0) retval = 360 + retval;
            return retval;
        }

        // Object commands

        ///<summary>Return a list of AliveObjects found within a specified radius</summary>
        ///<param name="radius">The radius (in meters) within which to look</param>
        ///<returns>A List of Prim objects</returns>
        private List<AliveObject> ObjectsAround(float radius)
        {
            logThis(radius.ToString());

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

            List<AliveObject> returnPrims = new List<AliveObject>();
            foreach (Primitive p in prims) {
                // convert OpenMetaverse Primitive to ALIVE Prim
                // but filter out ones we want to hide
                // Since all new objects in OpenSim default to
                // type "Primitive" we don't include those

                if (p.Properties != null)
                {
                    if (p.Properties.Name != null)
                    {
                        if (p.Properties.Name != "Primitive")
                            returnPrims.Add(new AliveObject(p));
                    }
                    else
                        returnPrims.Add(new AliveObject(p));
                }
                else
                    returnPrims.Add(new AliveObject(p));
            }
            return returnPrims;
        }

        /// <summary>Return a List of AliveObjects within a radius of 10 meters</summary>
        /// <returns>List of Prims</returns>
        public List<AliveObject> ObjectsAround()
        {
            return ObjectsAround(SEARCH_RADIUS);
        }

        /// <summary>Drop the specified object near where the avatar is standing</summary>
        /// <param name="item">The AliveObject to drop</param>
        public void DropObject(AliveObject item) 
        {
            logThis(item.ToString());

            client.Objects.DropObject(client.Network.CurrentSim, item.LocalID);
        }


        /// We will use "attach" to <summary>pick up an object by
        /// carrying it by hand, i.e. attach to left or right hand</summary>
        /// <param name="item">AliveObject to be picked up</param>
        public void PickupObject(AliveObject item) 
        {
            logThis(item.ToString());

            client.Objects.AttachObject(client.Network.CurrentSim, item.LocalID, AttachmentPoint.LeftHand, Quaternion.Identity);
        }


        // CHAT COMMANDS

        /// <summary>Get all the instant messages from the World Master since last checking</summary>
        /// <returns>The message(s) as a string</returns>
        public string GetMessage () 
        {
            logThis("");

            // sleep 30 seconds waiting for WM to type in message
            Thread.Sleep(30000);

            string temp = imb;
            imb = "";
            return temp; 
        }
        
        /// <summary>Send the specified message to the World Master</summary>
        /// <param name="message">The message to send World Master</param>
        public void SayMessage (string message) 
        {
            if (!message.StartsWith("[")) // prevent recursion
            logThis(message);

            ulong id = WorldMasterUUID.GetULong();
            // Find out UUID of World master by avatar name lookup
            if (WorldMasterUUID.GetULong() != 0L)
                client.Self.InstantMessage(WorldMasterUUID, message);
        }

        /// <summary>Get the messages in local chat since last checking</summary>
        /// <remarks>Local chat is within a 20 meter radius</remarks>
        /// <returns>A string containing messages seen in local chat, including your own.
        /// </returns><remarks>Messages include your own chat, and begin with avatar name colon</remarks>
        public string GetChat () 
        {
            logThis("");

            string temp = cb;
            cb = "";
            return temp;
        }

        /// <summary>Say the specified message in local chat</summary>
        /// <remarks>Local chat is heard within a 20 meter radius</remarks>
        /// <param name="message">The message to say</param>
        public void SayChat(string message) 
        {
            logThis(message);

            // Chat on channel 0 = nearby 20 m
            client.Self.Chat(message, 0, ChatType.Normal);
        }

        /// <summary>List the names of ALIVE properties of object
        /// </summary>
        /// <param name="p">Primitive whose properties are to be returned</param>
        private List<string> GetObjectProps(AliveObject p) {
            List<string> props = new List<string>();
            
            // props.Add("id");
            // props.Add("name");

            props.Add("x");
            props.Add("y"); // all Prims have coordinates
            if (p.shape != "unknown")
                props.Add("shape");
            props.Add("movable");
            if (p.color != "unknown")
                props.Add("color");
            if (p.name != "")
                props.Add("type");
            if (p.description != "")
                props.Add("other");
            if (p.shape == "box" || p.shape == "prism") {
                props.Add("orientation");
                props.Add("width");
                props.Add("depth");
            } else {
                props.Add("radius");
            }
            props.Add("height");

            return props;
        }

        /// <summary>Return the value of the named property for this Prim</summary>
        /// <param name="p">The Prim of interest</param>
        /// <param name="propName">The property to be returned</param>
        private string GetObjProp(AliveObject p, string propName)
        {
            String retval = "";
            switch(propName.ToLower()) {
                case "x": 
                    retval = p.X.ToString();
                    break;
                case "y": 
                    retval = p.Y.ToString();
                    break;
                case "shape": 
                    retval = p.shape.ToString();
                    break;
                case "movable":
                    retval = p.movable.ToString();
                    break;
                case "color":
                    retval = p.color;
                    break;
                case "type":
                case "name":
                    retval = p.name;
                    break;
                case "other":
                    retval = p.description;
                    break;
                case "orientation":
                    retval = p.angle.ToString();
                    break;
                case "radius":
                    retval = (p.width / 2).ToString(); // assume w = h
                    break;
                case "width":
                    retval = p.width.ToString();
                    break;
                case "depth":
                    retval = p.depth.ToString();
                    break;
                case "height":
                    retval = p.height.ToString();
                    break;
                // potentially needed to uniquely identify SL objects
                case "id":
                    retval = p.LocalID.ToString();
                    break;
                default:
                    break;
            }
            return retval;
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
        private void Network_OnConnected(Object sender)
        {
            System.Console.WriteLine("Successfully connected");
        }

        private void Self_OnInstantMessage(InstantMessage im, Simulator sim)
        {
            System.Console.WriteLine("Instant Message: " + im.Message);
            if (im.Message != "typing")
            imb = imb + im.FromAgentName + ": " + im.Message + "\r\n";
        }

        // Keep track of avatar names to find World Master UUID
        
        private void Self_OnNewAvatar(Simulator sim, Avatar av, ulong regionHandle, ushort timeDilation)
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

        private void Self_OnChat(string message, ChatAudibleLevel audible, ChatType type, ChatSourceType sourceType, string fromName, UUID id, UUID ownerid, Vector3 position)
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
