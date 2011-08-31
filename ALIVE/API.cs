using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.Utilities;
using System.Reflection;

namespace ALIVE
{
    /// <returns></returns>
    public enum primType
    {
        /// <summary>Unknown primitive type</summary>
        Unknown,
        /// <summary>Box primitive type</summary> 
        Box,
        /// <summary>Cylinder primitive type</summary>
        Cylinder,
        /// <summary>Prism primitive type</summary>
        Prism,
        /// <summary>Sphere primitive type</summary>
        Sphere,
        /// <summary>Torus primitive type</summary>
        Torus,
        /// <summary>Tube primitive type</summary>
        Tube,
        /// <summary>Ring primitive type</summary>
        Ring,
        /// <summary>Sculpted primitive type</summary>
        Sculpt
    };

    /// <summary>The most basic type of ALIVE object, akin to a Second Life Primitive</summary>
    public class AliveObject
    {
        /// <summary>
        ///This represents the kind of AliveObject this is, 
        /// for example Tree, House, Wall, Ball, Cube. 
        /// It is stored on the Description slot of the object as
        /// created in the virtual world
        /// </summary>
        public string family;
        /// <summary>
        ///64 bit Global ID (unique across the virtual world)
        /// </summary>
        private ulong ID;
        /// <summary>
        ///32 bit Local ID (unique within the current region)
        /// Although public, this is for internal use only.
        /// </summary>
        public uint LocalID;
        /// <summary>
        ///X coordinate within current region
        /// </summary>
        public float X;
        /// <summary>
        ///Y coordinate within current region
        /// </summary>
        public float Y;
        /// <summary>
        ///Primitive type (see primType)
        /// </summary>
        public string shape;
        /// <summary>
        ///Can the Prim be picked up or moved?
        /// </summary>
        public bool movable;
        /// <summary>
        ///String representing one of the colors: red, blue, green, yellow, aqua, purple, black, white.
        ///Other colors are not represented here and appear as "unknown"
        /// </summary>
        public string color;
        /// <summary>
        ///Name of the AliveObject as it appears in-world, for example $ball3
        /// </summary>
        public string name;
        /// <summary>
        ///Rotation of the primary face of the Prim around the vertical axis 
        ///         in degrees, measured counter-clockwise from due East.
        /// </summary>
        public float angle;
        /// <summary>
        ///Size of the Primitive in 3 dimensions
        /// </summary>
        public float width, depth, height;
        //public float colorR, colorG, colorB;

        /// <summary>
        /// ALIVE object representing an avatar
        /// </summary>
        /// <param name="av"></param>
        public AliveObject(Avatar av)
        {
            ID = av.ID.GetULong();
            LocalID = av.LocalID;
            X = av.Position.X;
            Y = av.Position.Y;
            family = "Avatar";
            height = av.Scale.Z;
            width = av.Scale.X;
            name = av.Name;
            angle = SmartDog.ZrotFromQuaternion(av.Rotation);
        }

        // Constructor
        /// <summary>The most basic type of ALIVE object, akin to a Second Life Primitive</summary>
        public AliveObject(Primitive p)
        {
            ID = p.ID.GetULong();
            LocalID = p.LocalID;

            X = p.Position.X;
            Y = p.Position.Y;

            width = p.Scale.X;
            depth = p.Scale.Y;
            height = p.Scale.Z;

            if (p.Textures != null)
            {
                color = SmartDog.color2String(
                    p.Textures.DefaultTexture.RGBA.R,
                    p.Textures.DefaultTexture.RGBA.G,
                    p.Textures.DefaultTexture.RGBA.B);
            }
            else
            {
                color = "undefined";
            }

            movable = (p.Flags & PrimFlags.ObjectMove) != 0;
            shape = p.Type.ToString();

            name = "";
            family = "";
            if (p.Properties != null)
            {
                if (p.Properties.Name != null)
                    name = p.Properties.Name;
                if (p.Properties.Description != null)
                    family = p.Properties.Description;
            }

            angle = SmartDog.ZrotFromQuaternion(p.Rotation);
        }



        /// <summary>Returns a printable description of Prim attributes</summary>
        public string toString()
        {
            // ALIVE objects use only LocalID, leave out global UUID
            // LocalID.ToString()
            return "<" + X.ToString("0.0") + "," + Y.ToString("0.0")
                + "> " + shape + " " +
                angle.ToString("0.0") +
                " [" + width.ToString("0.0") + "," + depth.ToString("0.0") + "," + height.ToString("0.0") + "] " +
                color + " " + movable + " " + name + " " + family;
        }
    }; // public class AliveObject


    /// <summary>Object which represents an avatar in ALIVE/OpenMetaverse/SecondLife</summary>
    public class SmartDog
    {
        // naughty 'globals'

        public string AliveVersion = "6/4/2011";
        public string ALIVE_SERVER = "http://ohio.lti.cs.cmu.edu:9000";
        const string SECONDLIFE_SERVER = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
        //const string WORLD_MASTER_NAME = "World Master";
        const int SEARCH_RADIUS = 25;
        const int walkSpeed = 320; // msec per meter

        private Dictionary<uint, Avatar> AvatarNames;
        private UUID WorldMasterUUID = new UUID(0L);
        private UUID DogMasterUUID = new UUID(0L);
        public Boolean logging = false;

        /// <summary>
        /// if not null, the AliveObject currently held by the avatar
        /// </summary>
        public AliveObject carriedObject = null;

        private string FirstName;
        private string LastName;
        private string Password;
        private string Simulator;
        private Boolean loggedIn = false;

        public GridClient client;

        public InventoryFolder CurrentDirectory = null;
        public Dictionary<UUID, AvatarAppearancePacket> Appearances = new Dictionary<UUID, AvatarAppearancePacket>();


        /// <summary>Construct a new SmartDog avatar</summary>
        /// <param name='fn'>first name</param>
        /// <param name='ln'>last name</param>
        /// <param name='pw'>password</param>>
        /// <param name='sim'>Region name to log into</param>
        public SmartDog(string fn, string ln, string pw, string sim)
        {
            FirstName = fn;
            LastName = ln;
            Password = pw;
            Simulator = sim;

            AvatarNames = new Dictionary<uint, Avatar>();

            client = new GridClient();
            Settings.LOG_LEVEL = Helpers.LogLevel.Debug;
            client.Settings.USE_LLSD_LOGIN = true;
            client.Settings.LOG_RESENDS = false;
            client.Settings.STORE_LAND_PATCHES = true;
            client.Settings.ALWAYS_DECODE_OBJECTS = true;
            client.Settings.ALWAYS_REQUEST_OBJECTS = true;
            client.Settings.SEND_AGENT_UPDATES = true;
            client.Settings.USE_ASSET_CACHE = true;
 
            NetworkManager Network = client.Network;

            Network.RegisterCallback(PacketType.AgentDataUpdate, AgentDataUpdateHandler);
            Network.LoginProgress += LoginHandler;
            client.Self.IM += Self_IM;
            client.Self.ChatFromSimulator += Self_OnChat;
            //client.Groups.GroupMembersReply += GroupMembersHandler;
            //Inventory.InventoryObjectOffered += Inventory_OnInventoryObjectReceived;

            Network.RegisterCallback(PacketType.AvatarAppearance, AvatarAppearanceHandler);
            Network.RegisterCallback(PacketType.AlertMessage, AlertMessageHandler);

            //client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnected);

            client.Objects.KillObject += Objects_OnObjectKilled;
            client.Objects.AvatarUpdate += Self_OnNewAvatar;
                //client.Self.OnMeanCollision += new AgentManager.MeanCollisionCallback(Self_OnMeanCollision);

            // Register callback to catch Object properties events
            client.Objects.ObjectProperties += Objects_OnObjectProperties;
        }

        private void AvatarAppearanceHandler(object sender, PacketReceivedEventArgs e)
        {
            Packet packet = e.Packet;

            AvatarAppearancePacket appearance = (AvatarAppearancePacket)packet;

            lock (Appearances) Appearances[appearance.Sender.ID] = appearance;
        }

        private void AlertMessageHandler(object sender, PacketReceivedEventArgs e)
        {
            Packet packet = e.Packet;

            AlertMessagePacket message = (AlertMessagePacket)packet;

            Console.WriteLine("[AlertMessage] " + Utils.BytesToString(message.AlertData.Message), Helpers.LogLevel.Info, this);
        }

        void Self_IM(object sender, InstantMessageEventArgs e)
        {
            InstantMessage im = e.IM;
            System.Console.WriteLine("Instant Message: " + im.Message);
            if (im.Message != "typing")
                imb = imb + im.FromAgentName + ": " + im.Message + "\r\n";
        }

        private void AgentDataUpdateHandler(object sender, PacketReceivedEventArgs e)
        {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
            if (p.AgentData.AgentID == e.Simulator.Client.Self.AgentID)
            {
                //GroupID = p.AgentData.ActiveGroupID;

                //GroupMembersRequestID = e.Simulator.Client.Groups.RequestGroupMembers(GroupID);
            }
        }

        /// <summary>
        /// Initialize everything that needs to be initialized once we're logged in.
        /// </summary>
        /// <param name="login">The status of the login</param>
        /// <param name="message">Error message on failure, MOTD on success.</param>
        public void LoginHandler(object sender, LoginProgressEventArgs e)
        {
            if (e.Status == LoginStatus.Success)
            {
                // Start in the inventory root folder.
                CurrentDirectory = client.Inventory.Store.RootFolder;

                    // update location, avatars, objects nearby here?
                    loggedIn = true;

                // This is what seems to have solved the cloud avatar problem
                // Unfortunately it causes another problem: creating the cache
                // folder wherever the EXE is run, which has permission problems
                // which case appearance data to go away
                //client.Appearance.SetPreviousAppearance(false);

                //client.Network.CurrentSim.ObjectsAvatars.ForEach(OpenMetaverse.Avatar.AvatarProperties.Equals);

                // This takes some time to ensure prims show up
                // otherwise the carried item hasn't appeared yet in the sim
                // dictionary (how do we know when all objects nearby have apeared?
                // possibly never, as movement within the sim is always causing new
                // ones to appear asynchronously)
                this.ObjectsAround();

                // Now that we have a pretty good sense the objects are present,
                // drop anything whose parent ID is the avatar, and is held in
                // left hand
                dropCarriedItem();

                logThis("");
            }
        }

        // methods

        private void logThis(String args)
        {
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

        /// <summary>Attempt to log the avatar into the default region</summary>
        public bool Login()
        {
            Boolean LoginSuccess;

            LoginParams loginParams = new LoginParams();
            loginParams = client.Network.DefaultLoginParams(FirstName, LastName, Password, "ALIVE", "Bot");
            loginParams.Start = "uri:" + Simulator + "&128&128&26"; // specify start location.  We set avatars homes at 128,128
            loginParams.URI = ALIVE_SERVER;
            LoginSuccess = 
                client.Network.Login(loginParams);

            //client.Network.CurrentSim.ObjectsAvatars.ForEach();
            //client.Network.CurrentSim.ObjectsPrimitives.ForEach();

                return true;
        }

        /// <summary>Log the avatar out</summary>
        public bool Logout()
        {
            logThis("");
            if (loggedIn) client.Network.Logout();

            return true;
        }

        // Animation commands
        /// <summary>Play an animation (sleeps 5 seconds while playing)</summary>
        public bool PlayAnimation(UUID anim)
        {
            
            client.Self.AnimationStart(anim, false);
            Thread.Sleep(5000);
            client.Self.AnimationStop(anim, false);
            return true;
        }

        /// <summary>Play an animation (sleeps 5 seconds while playing)</summary>
        /// <param name="animName">One of the possible AliveAnimation.animationNames (case insensitive)</param>
        /// <returns>true if succeeded, false if named animation doesn't exist</returns>
        public bool PlayAnimation(String animName)
        {
            animName = animName.ToUpper();
            if (!AliveAnimation.animationNames.Contains(animName))
                return false;

            FieldInfo fi = typeof(AliveAnimation).GetField(animName,
                BindingFlags.Public | BindingFlags.Static);

            if (fi != null)
                return(PlayAnimation((OpenMetaverse.UUID)fi.GetValue(null)));
            else
                return false;
        }

        // Animation commands
        /// <summary>Start playing an animation, then return immediately</summary>
        /// <param name="anim">OpenMetaverse.UUID of the animation to play (see AliveAnimation)</param>
        public void StartAnimation(UUID anim)
        {
            client.Self.AnimationStart(anim, false);
        }

        // Animation commands
        /// <summary>Stop playing an animation (the one started with StartAnimation)</summary>
        public void StopAnimation(UUID anim)
        {
            client.Self.AnimationStop(anim, false);
        }

        // Movement commands

        /// <summary>Rotate the avatar to face a specified location at the avatar's current Z elevation</summary>
        /// <param name='x'>X coordinate</param>
        /// <param name='y'>Y coordinate</param>
        public void TurnTo(float x, float y)
        {
            TurnTo(x, y, client.Self.SimPosition.Z);
        }

        /// <summary>Rotate the avatar to face a specified 3d location</summary>
        /// <param name='x'>X coordinate</param>
        /// <param name='y'>Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        public void TurnTo(float x, float y, float z)
        {
            logThis(x + "," + y + "," + z);

            Vector3 position = new Vector3(x, y, z);

            client.Self.Movement.TurnToward(position);
        }

        /// <summary>Rotate the avatar counter-clockwise</summary>
        /// <param name='degrees'>degrees to rotate</param>
        public bool TurnLeft(long degrees)
        {
            logThis(degrees.ToString());

            float angle = (float)Math.PI * (float)degrees / -360f;

            Quaternion rot = new Quaternion(0, 0, (float)Math.Cos(angle / 2), (float)Math.Sin(angle / 2));

            // I don't know why but doing it twice makes it work            
            Quaternion q = client.Self.Movement.BodyRotation * rot * rot;

            client.Self.Movement.BodyRotation = q;
            client.Self.Movement.HeadRotation = q;
            client.Self.Movement.SendUpdate(true);

            return true;
        }

        /// <summary>Rotate the avatar clockwise</summary>
        /// <param name='degrees'>degrees to rotate</param>
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

        /// <summary>
        /// Stop moving
        /// </summary>     
        public void stopMoving()
        {
            client.Self.Movement.AtPos = false;
            client.Self.Movement.SendUpdate(true);
            client.Self.AutoPilotCancel();
            client.Self.Fly(false);
        }

        /// <summary>Attempt to walk the avatar forward in a straight line.  Obstacles may prevent this from completing as expected</summary>
        /// <param name='milliseconds'>Time to spend walking</param>
        public void WalkForward(int milliseconds)
        {
            client.Self.Movement.AtPos = true;
            client.Self.Movement.SendUpdate(true);

            Thread.Sleep(milliseconds);
            client.Self.Movement.AtPos = false;
            client.Self.Movement.SendUpdate(true);
        }

        /// <summary>Attempt to walk the avatar backward in a straight line.  Obstacles may prevent this from completing as expected</summary>
        /// <param name='milliseconds'>Time to spend walking</param>
        public void WalkBackward(int milliseconds)
        {
            client.Self.Movement.AtNeg = true;
            client.Self.Movement.SendUpdate(true);

            Thread.Sleep(milliseconds);
            client.Self.Movement.AtNeg = false;
            client.Self.Movement.SendUpdate(true);
        }

        /// <summary>Attempt to nudge the avatar forward in a straight line.  Obstacles may prevent this from completing as expected</summary>
        /// <param name='milliseconds'>Time to spend being nudged</param>
        public void NudgeForward(int milliseconds)
        {
            client.Self.Movement.NudgeAtPos = true;
            client.Self.Movement.SendUpdate(true);

            Thread.Sleep(milliseconds);
            client.Self.Movement.NudgeAtPos = false;
            client.Self.Movement.SendUpdate(true);
        }

        /// <summary>Attempt to nudge the avatar backward in a straight line.  Obstacles may prevent this from completing as expected</summary>
        /// <param name='milliseconds'>Time to spend being nudged</param>
        public void NudgeBackward(int milliseconds)
        {
            client.Self.Movement.NudgeAtNeg = true;
            client.Self.Movement.SendUpdate(true);

            Thread.Sleep(milliseconds);
            client.Self.Movement.NudgeAtNeg = false;
            client.Self.Movement.SendUpdate(true);
        }

        // Overload
        public bool GoForward(int meters)
        {
            return GoForward(Convert.ToSingle(meters));
        }

        /// <summary>Attempt to walk the avatar forward in a straight line.  Obstacles may prevent this from completing as expected</summary>
        /// <param name='meters'>Distance to walk in meters</param>
        public bool GoForward(float meters)
        {
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

            targetX = x + (float)(meters * Math.Sin(angle / 180 * Math.PI));
            targetY = y + (float)(meters * Math.Cos(angle / 180 * Math.PI));

            client.Self.Movement.AtPos = true;
            client.Self.Movement.SendUpdate(true);

            Thread.Sleep(Convert.ToInt16(walkSpeed * meters));
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

        // Overload
        public bool GoBackward(int meters)
        {
            return GoBackward(Convert.ToSingle(meters));
        }

        /// <summary>Attempt to walk the avatar backwards in a straight line.  Obstacles may prevent this from completing as expected</summary>
        /// <param name='meters'>Distance to walk in meters</param>
        public bool GoBackward(float meters)
        {
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

            Thread.Sleep(Convert.ToInt16(walkSpeed * meters));
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

        /// <summary>Go to the specified location.</summary>
        /// <remarks>This is not very reliable, in Second Life, or in ALIVE, and can 
        ///result in the avatar getting stuck.  Use with caution.  This routine returns
        ///after the time taken to travel this distance,
        ///assuming a travel speed of 3 meters per second.</remarks>
        ///
        /// <param name="x">X coordinate of location to attempt to travel to</param>
        /// <param name="y">Y coordinate of location to attempt to travel to</param>
        /// <returns>True or false depending on whether the location was reached (within a margin of error of 0.8 meters - from experimental data)</returns>
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
            Thread.Sleep(Convert.ToInt32(320 * distance) + 500);

            // Stop moving and/or flying (if something went horribly wrong
            // SecondLife "pops up" avatars into flying stance.
            client.Self.AutoPilotCancel();
            client.Self.Fly(false);
            pos = client.Self.SimPosition;

            Console.Out.WriteLine("Completed autopilot at: " + pos.X + "," + pos.Y);

            // Need to make these fuzzy; final location not exact
            float fuzz = 0.8f;
            if ((Math.Abs(pos.X - x) < fuzz) && (Math.Abs(pos.Y - y) < fuzz))
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
        /// 
        List<Primitive> getAttachments() {
            List<Primitive> attachments = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                    delegate(Primitive prim) { return prim.ParentID == client.Self.LocalID; }
                );

            return attachments;
        }

        // Drop whatever is attached to the avatar's spine
        private bool dropCarriedItem()
        {
            List<Primitive> heldItems = getAttachments();
            foreach (Primitive p in heldItems)
            {
                if (p.PrimData.AttachmentPoint == AttachmentPoint.Spine)
                {
                    //Console.Out.WriteLine("Dropping " + p.LocalID);
                    client.Objects.DropObject(client.Network.CurrentSim, p.LocalID);
                    return true;
                }
            }
            return false;
        }

        // Drop whatever is attached to specified point
        private bool dropCarriedItem(AttachmentPoint point)
        {
            List<Primitive> heldItems = getAttachments();
            foreach (Primitive p in heldItems)
            {
                if (p.PrimData.AttachmentPoint == point)
                {
                    //Console.Out.WriteLine("Dropping " + p.LocalID);
                    client.Objects.DropObject(client.Network.CurrentSim, p.LocalID);
                    return true;
                }
            }
            return false;
        }

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
        public static float ZrotFromQuaternion(Quaternion q)
        {
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

        /// <summary>Return a list of AliveObjects found within a specified radius</summary>
        /// <param name="radius">The radius (in meters) within which to look</param>
        /// <returns>A List of Prim objects</returns>
        private List<AliveObject> ObjectsAround(float radius)
        {
            logThis(radius.ToString());

            Vector3 location = client.Self.SimPosition;
            bool flag;
            // double tripleRadius = 3 * (double)radius;

            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    int r3 = 3 * (int)radius;
                    //                    return ((prim.ParentID == 0) && (pos != Vector3.Zero));
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < r3));
                });

            // *** request properties of (only) these objects ***
            bool complete = RequestObjectProperties(prims, 500, true);

            //Console.Out.WriteLine("Properties completed: " + complete);

            List<AliveObject> returnPrims = new List<AliveObject>();
            foreach (Primitive p in prims)
            {
                // convert OpenMetaverse Primitive to ALIVE Prim
                // but filter out ones we want to hide
                // Since all new objects in OpenSim default to
                // type "Primitive" we don't include those

                flag = (p.Properties != null) && (p.Properties.Name != null) && (p.Properties.Name == "Primitive");
                if (flag == false)
                    if (visible(p, location, radius))
                        returnPrims.Add(new AliveObject(p));
                
            }
            return returnPrims;
        }

        private bool visible(Primitive p, Vector3 location, float radius)
        // determines if any part of the object p is visible to the avatar from location
        {

            double xa, ya, xa1, ya1, xa2, ya2, xc, yc, w, d, angle, r2;
            xa = location.X;
            ya = location.Y;
            xc = p.Position.X;
            yc = p.Position.Y;
            w = p.Scale.X / 2;
            d = p.Scale.Y / 2;
            angle = Math.PI * SmartDog.ZrotFromQuaternion(p.Rotation) / 180;
            r2 = radius * radius;
            // transform to object-centered coordinates, consider all objects rectangular
            xa1 = xa - xc;
            ya1 = ya - yc;
            if (xa1 * xa1 + ya1 * ya1 <= r2) return true;
            xa2 = xa1 * Math.Cos(angle) + ya1 * Math.Sin(angle);
            ya2 = -xa1 * Math.Sin(angle) + ya1 * Math.Cos(angle);
            if (xa2 <= -w)
            {
                if (ya2 <= -d) return (xa2 + w) * (xa2 + w) + (ya2 + d) * (ya2 + d) <= r2;
                if (ya2 >= d) return (xa2 + w) * (xa2 + w) + (ya2 - d) * (ya2 - d) <= r2;
                return xa2 + w >= -radius;
            }
            else
            {
                if (xa2 >= w)
                {
                    if (ya2 <= -d) return (xa2 - w) * (xa2 - w) + (ya2 + d) * (ya2 + d) <= r2;
                    if (ya2 >= d) return (xa2 - w) * (xa2 - w) + (ya2 - d) * (ya2 - d) <= r2;
                    return xa2 - w <= radius;
                }
                else
                {
                    if (ya2 >= d) return ya2 - d <= radius;
                    if (ya2 <= -d) return ya2 + d <= -radius;
                    return true; // the avatar is inside the rectangle footprint of the object
                }
            }
        }

        /// <summary>Return a List of AliveObjects within a radius of 10 meters</summary>
        /// <returns>List of Prims</returns>
        public List<AliveObject> ObjectsAround()
        {
            // There seems to be a bug when calling this the first
            // time.  The object properties such as Name and Description
            // come back blank.  Workaround:  do it twice.  Knowing the asynchronous
            // nature of SecondLife, it is asking too much to attempt to
            // debug they "why" of this bug.  Given time & experimentation
            // maybe we can work it out :)
            List<AliveObject> tempObjects = ObjectsAround(SEARCH_RADIUS);

            // Here is the 'magic' workaround - try again after 1s delay
            Thread.Sleep(1000);

            // Also look for avatars nearby
            Vector3 loc = client.Self.SimPosition;
            Dictionary<uint, Avatar>.Enumerator en = AvatarNames.GetEnumerator();

            // Do it again
            tempObjects = ObjectsAround(SEARCH_RADIUS);

            // Add Avatars
            lock (AvatarNames)
                foreach (KeyValuePair<uint, Avatar> kp in AvatarNames)
                {
                    Avatar av = kp.Value;

                    if (av.ID != client.Self.AgentID)
                    {
                        float dist = Vector3.Distance(av.Position, client.Self.SimPosition);
                        if (dist <= SEARCH_RADIUS)
                        {
                            AliveObject obj = new AliveObject(av);
                            tempObjects.Add(obj);
                        }
                    }
                }

            return tempObjects;
        }

        /// <summary>Drop the specified object near where the avatar is standing</summary>
        /// <param name="item">The AliveObject to drop</param>
        public bool DropObject(AliveObject item)
        {
            logThis(item.ToString());

            return dropCarriedItem();
        }
        /// <summary>Drop the currently-carried object near where the avatar is standing</summary>
        public bool DropObject()
        {
            logThis("");
            return dropCarriedItem();
        }


        /// We will use "attach" to <summary>pick up an object by
        /// carrying it by hand, i.e. attach to left hand
        /// so long as the object is within 5 meters of the avatar</summary>
        /// <param name="item">AliveObject to be picked up</param>
        public bool PickupObject(AliveObject item)
        {
            logThis(item.ToString());

            if (item.movable == false)
                return false;

            // Don't let avatar pick up objects farther away than 5m
            Vector3 avatarPosition = client.Self.SimPosition;
            Vector3 objectPosition = new Vector3(item.X, item.Y, avatarPosition.Z);

            if (Vector3.Distance(avatarPosition, objectPosition) < 5)
            {
                client.Objects.AttachObject(client.Network.CurrentSim, item.LocalID, AttachmentPoint.Spine, Quaternion.Identity);
                carriedObject = item;
                return true;
            }
            else
                return false;
        }

        /// We will use "attach" to <summary>pick up an object by
        /// attaching to specified AttachmentPoint
        /// so long as the object is within 5 meters of the avatar</summary>
        /// <param name="item">AliveObject to be picked up</param>
        /// <param name="point">AttachmentPoint to use</param>
        public bool PickupObject(AliveObject item, AttachmentPoint point)
        {
            logThis(item.ToString());

            if (item.movable == false)
                return false;

            // Don't let avatar pick up objects farther away than 5m
            Vector3 avatarPosition = client.Self.SimPosition;
            Vector3 objectPosition = new Vector3(item.X, item.Y, avatarPosition.Z);

            if (Vector3.Distance(avatarPosition, objectPosition) < 5)
            {
                client.Objects.AttachObject(client.Network.CurrentSim, item.LocalID, point, Quaternion.Identity);
                carriedObject = item;
                return true;
            }
            else
                return false;
        }


        // CHAT COMMANDS

        /// <summary>Get all the instant messages from the World Master since last checking</summary>
        /// <returns>The message(s) as a string</returns>
        public string GetMessage()
        {
            logThis("");

            // wait up to 60 seconds polling for message from World Master (or Dog Master)
            Int16 i = 0;
            do
            {
                Thread.Sleep(5000);
                i++;
            } while (i < 12 && imb == "");

            string temp = imb;
            imb = "";
            return temp;
        }

        /// <summary>Send the specified message to the World Master</summary>
        /// <param name="message">The message to send World Master</param>
        public void SayMessage(string message)
        {
            if (!message.StartsWith("[")) // prevent recursion
                logThis(message);

            // Find out UUID of World master by avatar name lookup
            if (WorldMasterUUID.GetULong() != 0L)
                client.Self.InstantMessage(WorldMasterUUID, message);
            if (DogMasterUUID.GetULong() != 0L)
                client.Self.InstantMessage(DogMasterUUID, message);
        }

        /// <summary>Get the messages in local chat since last checking</summary>
        /// <remarks>Local chat is within a 20 meter radius</remarks>
        /// <returns>A string containing messages seen in local chat, including your own.
        /// </returns><remarks>Messages include your own chat, and begin with avatar name and a colon</remarks>
        public string GetChat()
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
        private List<string> GetObjectProps(AliveObject p)
        {
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
            if (p.family != "")
                props.Add("other");
            if (p.shape == "box" || p.shape == "prism")
            {
                props.Add("orientation");
                props.Add("width");
                props.Add("depth");
            }
            else
            {
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
            switch (propName.ToLower())
            {
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
                    retval = p.family;
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

        void Objects_OnObjectProperties(Object sender,  ObjectPropertiesEventArgs properties)
        {
            UUID ObjectID = properties.Properties.ObjectID;
            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(ObjectID, out prim))
                {
                    prim.Properties = properties.Properties;
                    PrimsWaiting.Remove(ObjectID);
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

        //// Attempt to wear the inventory item named by the argument itemName
        //// Does not report error on failure
        //public void lookupCarriedItem()
        //{
        //    //initialize our list to store the folder contents
        //    UUID ObjectsFolderUUID;
        //    List<InventoryBase> contents;

        //    //make a string array to put our folder names in.
        //    String[] SearchFolders = { "" };

        //    //Next we grab a full copy of the entire inventory and get it stored into the Inventory Manager
        //    client.Inventory.RequestFolderContents(client.Inventory.Store.RootFolder.UUID, client.Self.AgentID, true, true, InventorySortOrder.ByDate);

        //    //Now we can grab the details of that folder and store it to our list.
        //    ObjectsFolderUUID = client.Inventory.FindObjectByPath(client.Inventory.Store.RootFolder.UUID, 
        //        client.Self.AgentID, "Objects", 1000);

        //    // Create the list which we can use to iliterate through
        //    contents =
        //        client.Inventory.FolderContents(ObjectsFolderUUID, client.Self.AgentID,
        //            true, true, InventorySortOrder.ByName, 1000);


        //    foreach (InventoryBase item in contents)
        //    {
        //        // Illiterate through our list of items and print them to the console

        //        // Code that processes each item goes here
        //        InventoryItem myitem = client.Inventory.FetchItem(item.UUID, item.OwnerID, 1000);
        //        if (myitem != null)
        //        {
        //            Console.Out.WriteLine("Name: " + myitem.Name + " <==> " + myitem.UUID.ToString());
        //            Console.Out.WriteLine("Flags: " + myitem.Flags);
        //            //Console.Out.WriteLine(myitem is InventoryAttachment);
        //            if (myitem is InventoryAttachment)
        //                Console.Out.WriteLine(((InventoryAttachment)myitem).AttachmentPoint.ToString());
        //        }
        //    }


        //    //now that we have the details of the objects folder, we need to grab the details of our torch.
        //    //SearchFolders[0] = itemName;
        //    //inventoryItems = client.Inventory.FindObjectByPath(inventoryItems, client.Self.AgentID, SearchFolders[0], 1000);

        //    //InventoryItem myitem = null;

        //    // Convert the LLUUID to an inventory item
        //    //myitem = client.Inventory.FetchItem(inventoryItems, client.Self.AgentID, 1000);

        //    //finally we attach the object to it's default position
        //    try
        //    {
        //        // Catch any errors that may occur (not having the "Torch!" item in your inventory for example)
        //        //client.Appearance.Attach(myitem as InventoryItem, AttachmentPoint.Default);
        //    }
        //    catch
        //    {
        //        // Put any code that handles any errors :)
        //        System.Console.WriteLine("Error looking up items");
                
        //    }
        //}

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

        private void Objects_OnAvatarAttachment(Simulator simulator, Primitive prim, ulong regionHandle,
            ushort timeDilation)
        {
            Console.WriteLine(prim.ParentID + " " + client.Self.LocalID);
            Console.WriteLine("Got an attachment!" + prim.PrimData.AttachmentPoint.ToString() + "\r\n" + prim.ToString());
        }

        private void Self_OnNewAvatar(Object sender, AvatarUpdateEventArgs e)
            //Simulator sim, Avatar av, ulong regionHandle, ushort timeDilation)
        {
            Avatar av = e.Avatar;
            string avatarName = av.FirstName + " " + av.LastName;

            // DEBUG
            Console.WriteLine("Self_OnNewAvatar: " + avatarName);

            if (AvatarNames.ContainsKey(av.LocalID))
                return;
            lock (AvatarNames)
                AvatarNames.Add(av.LocalID, av);

            if (avatarName == "Master " + LastName)
                DogMasterUUID = av.ID;
            if (avatarName == "World Master")
                WorldMasterUUID = av.ID;
        }

        // Strangely there's no event for Avatars leaving, it's bundled with
        // objects.
        private void Objects_OnObjectKilled(Object sender, KillObjectEventArgs args)
        {
            // DEBUG
            Console.WriteLine("OnObjectKilled: ");
            uint objectID = args.ObjectLocalID;

            lock (AvatarNames)
            {
                if (AvatarNames.ContainsKey(objectID))
                {
                    // DEBUG
                    Avatar av;
                    AvatarNames.TryGetValue(objectID, out av);
                    Console.WriteLine("Removing " + av.Name);
                    AvatarNames.Remove(objectID);
                }
            }
        }


        // Chat buffers
        //
        // cb - chat buffer
        // imb - instant message buffer
        private static string cb;
        private static string imb;

        private void Self_OnChat(Object sender, ChatEventArgs chatArgs)
        {
            String message = chatArgs.Message;
            ChatType type = chatArgs.Type;
            String fromName = chatArgs.FromName;
            ChatAudibleLevel audible = chatArgs.AudibleLevel;
            ChatSourceType sourceType = chatArgs.SourceType;

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