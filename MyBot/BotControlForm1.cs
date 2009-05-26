using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using OpenMetaverse;

using HttpServer;
using System.Net;
using HttpListener = HttpServer.HttpListener;

namespace MyBot
{

    public partial class BotControlForm1 : Form
    {
        private static GridClient client;
        private static bool LoginSuccess = false;
        private static int chnum = 0;
        private string msg;
        private string BotAvatarName = "";
        private static List<String> AvatarNames = new List<String>();

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        //int[,] Map;

        // The delegate and the funny-looking method solved an annoying problem of
        // not being able to update the ChatBox from another thread – the one that
        // responds to the OnChat events
        public delegate void textBoxUpdater(TextBox tb, string nvalue);

        public void textBoxUpdate(TextBox tb, string nvalue)
        {
            if (tb.InvokeRequired)
            {
                tb.Invoke(new textBoxUpdater(textBoxUpdate), new object[] { tb, nvalue });
                return;
            }
            tb.Text = nvalue;
            tb.SelectionStart = tb.Text.Length;
            tb.ScrollToCaret();
            //tb.Refresh();
        }

        public BotControlForm1()
        {
            InitializeComponent();
            client = new GridClient();

            SendButton.Click += new EventHandler(SendButton_Click);
            QuitButton.Click += new EventHandler(QuitButton_Click);
            MoveButton.Click += new EventHandler(MoveButton_Click);
            LookButton.Click += new EventHandler(LookButton_Click);

            client.Self.OnMeanCollision += new AgentManager.MeanCollisionCallback(Self_OnMeanCollision);
            client.Self.OnChat += new AgentManager.ChatCallback(Self_OnChat);
            client.Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Self_OnNewAvatar);

            // Register callback to catch Object properties events
            client.Objects.OnObjectProperties += new ObjectManager.ObjectPropertiesCallback(Objects_OnObjectProperties);

            //force logout and exit when form is closed
            this.FormClosing += new FormClosingEventHandler(BotControl_FormClosing);
        }

/////// SUBROUTINES

        // Return a list of prims within radius
        List<Primitive> lookAroundYou(float radius)
        {
            Vector3 location = client.Self.SimPosition;
            
            List<Primitive> prims = client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                delegate(Primitive prim)
                {
                    Vector3 pos = prim.Position;
                    return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                });

            //// Attempt to fill a grid with stuff found
            //int width = Convert.ToInt32(radius);
            //int[,] grid = new int[width*2+1,width*2+1];

            //// Save the global for path planning to "see"
            //Map = grid;

            //for (int i = 0; i < prims.Count; i++)
            //{
            //    int gridX = Convert.ToInt32(prims[i].Position.X - location.X + width) - 1;
            //    int gridY = Convert.ToInt32(prims[i].Position.Y - location.Y + width) - 1;
            //    grid[gridX, gridY] = -1;
            //}
            //for (int i = 0; i < width * 2; i++)
            //{
            //    for (int j = 0; j < width * 2; j++)
            //    {
            //        if (grid[i, j] != -1)
            //        {
            //            Console.Write(".");
            //            grid[i, j] = 1;
            //        }
            //        else Console.Write("*");
            //    }
            //    Console.WriteLine();
            //}
             //   Console.WriteLine("Prim " + i + prettyLocation(prims[i].Position));

            Logger.Log("Found " + prims.Count + " objects within " + radius + "m", Helpers.LogLevel.Info);

            // *** request properties of (only) these objects ***
            bool complete = RequestObjectProperties(prims, 250, false);

            return (prims);
        }

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


        string prettyLocation(Vector3 p)
        {
            return ("<" + p.X.ToString("0") + "," +
                          p.Y.ToString("0") + "," +
                          p.Z.ToString("0") + ">");
        }

        string prettySize(Vector3 p) {
            return(
            "(" + p.X.ToString("0.0") + ","
            + p.Y.ToString("0.0") + ","
            + p.Z.ToString("0.0") + ")"
            );
        }

        void displayMyLocation()
        {
            string myLocation = prettyLocation(client.Self.SimPosition);
            textBoxUpdate(locationBox, myLocation);
        }


        String GetBotAnswer(String inputstring, String fromName)
        {
            const String botID = "f5d922d97e345aa1"; // this is the Loebner prizewinning ALICE chatbot
            const String botURL = "http://www.pandorabots.com/pandora/talk-xml?botid=" + botID;

            // used to build entire input
            StringBuilder sb = new StringBuilder();

            // used on each read operation
            byte[] buf = new byte[8192];

            String myUri = botURL + "&input="
                + Uri.EscapeDataString(inputstring) + "&custid=" + 
                Uri.EscapeDataString(fromName);

            HttpWebRequest request;
            HttpWebResponse response;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(myUri);
            } catch {
                System.Console.WriteLine("Exception in Chatbot HTTP request");
                return "";
            }

            // execute the request
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch
            {
                System.Console.WriteLine("Exception in Chatbot HTTP response");
                return "";
            }

            // we will read data via the response stream
            Stream resStream = response.GetResponseStream();

            string tempString = null;
            int count = 0;

            do
            {
                // fill the buffer with data
                count = resStream.Read(buf, 0, buf.Length);

                // make sure we read some data
                if (count != 0)
                {
                    // translate from bytes to ASCII text
                    tempString = Encoding.ASCII.GetString(buf, 0, count);

                    // continue building the string
                    sb.Append(tempString);
                }
            }
            while (count > 0); // any more data to read?

            // clean up the output
            String buf2 = sb.ToString();
            int start = buf2.IndexOf("<that>");
            int end = buf2.IndexOf("</that>");
            String msglines = buf2.Substring(start + 6, (end-start) - 6);

            // Clean up HTML entities
            msglines = msglines.Replace("&lt;br&gt;", "\r");
            msglines = msglines.Replace("&quot;", "\"");
            msglines = msglines.Replace("&lt;", "<");
            msglines = msglines.Replace("&gt;", ">");

            // Delay 7 seconds
            Thread.Sleep(7000);

            // log and return response
            Console.WriteLine(msglines);
            return msglines;
        }


        // override
        public String GetBotAnswer(String inputstring)
        {
            return GetBotAnswer(inputstring, "12334");
        }


//////  EVENT HANDLERS

        void QuitButton_Click(object sender, EventArgs e)
        {
            if (QuitButton.Text == "Login")
            {

                LoginParams loginParams = new LoginParams();
                loginParams = client.Network.DefaultLoginParams(FNtextBox.Text, LNtextBox.Text, PWtextBox.Text, "My Bot", "Test User");
                loginParams.FirstName = FNtextBox.Text;
                loginParams.LastName = LNtextBox.Text;
                loginParams.Password = PWtextBox.Text;
                loginParams.Start = URIbox.Text;
                if (OpenSimCheckbox.Checked)
                    loginParams.URI = "http://localhost:9000";
                else
                    loginParams.URI = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";
                LoginSuccess = client.Network.Login(loginParams);

                //client.Network.CurrentSim.ObjectsAvatars.ForEach();
                //client.Network.CurrentSim.ObjectsPrimitives.ForEach();
                
                if (LoginSuccess)
                {
                    QuitButton.Text = "Logout";
                    ChatBox.Visible = true;
                    InputBox.Visible = true;
                    SendButton.Visible = true;
                    PWtextBox.Text = "";
                    BotAvatarName = FNtextBox.Text + " " + LNtextBox.Text;
                    //ChatBox.Text = "Chat around " + fname;
                    displayMyLocation();
                }
                else
                {
                    MessageBox.Show("Din't work! " + client.Network.LoginMessage);
                    return;
                }

                // This is what seems to have solved the Rez problem
                client.Appearance.SetPreviousAppearance(false);

                // Object Sensor attachment test
                //wearNamedItem("Object Sensor 3");

                displayMyLocation();
            }
            else
            {
                client.Network.Logout();
                QuitButton.Text = "Login";
                ChatBox.Visible = false;
                InputBox.Visible = false;
                SendButton.Visible = false;

                objectsBox.Text = "";
                locationBox.Text = "";
                avatarsBox.Text = "";
            }
        }


        void LookButton_Click(object sender, EventArgs e)
        {
            textBoxUpdate(objectsBox, "");
            LookButton.Enabled = false;

            float radius = Convert.ToInt32(radiusBox.Text);
            List<Primitive> prims = lookAroundYou(radius);

            Logger.Log("Got " + prims.Count + " objects back", Helpers.LogLevel.Info);

            // Synchronous object info update
            //String message ="";
            //for (int i = 0; i < prims.Count; i++)
            //    message += prims[i].Properties.Name + " " + prims[i].Position.ToString() + "\r\n";

            //textBoxUpdate(objectsBox, message);

            LookButton.Enabled = true;
        }


        void SendButton_Click(object sender, EventArgs e)
        {
            msg = InputBox.Text;
            client.Self.Chat(msg, chnum, ChatType.Normal);
            InputBox.Text = "";
        }


        void MoveButton_Click(object sender, EventArgs e)
        {
            Vector3 pos = client.Self.SimPosition;
            int goalX = Convert.ToInt32(Xbox.Text);
            int goalY = Convert.ToInt32(Ybox.Text);

            int x = Convert.ToInt32(pos.X) + goalX;
            int y = Convert.ToInt32(pos.Y) + goalY;
            client.Self.AutoPilotLocal(x, y, pos.Z);

            displayMyLocation();
        }


////  CALLBACKS

        // Other ways to detect objects/avatars within view radius:
        //client.Network.CurrentSim.ObjectsAvatars.ForEach();
        //client.Network.CurrentSim.ObjectsPrimitives.ForEach();

        public void Self_OnNewAvatar(Simulator sim, Avatar av, ulong regionHandle, ushort timeDilation)
        {
            string cb;
            string ncb;
            cb = avatarsBox.Text;
            string avatarName = av.FirstName + " " + av.LastName;

            if (AvatarNames.Contains(avatarName)) return;
            AvatarNames.Add(avatarName);

            Vector3 loc = client.Self.SimPosition;
            float dist = Vector3.Distance(av.Position, client.Self.SimPosition);
            if (dist <= 20)
            {
                ncb = cb + "\r\n" + dist.ToString("0") + "m" + " " + avatarName;
                textBoxUpdate(avatarsBox, ncb);

                if (ChatBotCheckbox.Checked && BotAvatarName != avatarName)
                {
                    String msg = GetBotAnswer("My name is " + av.FirstName, avatarName);
                    if (msg != "")
                    client.Self.Chat(msg, chnum, ChatType.Normal);
                }
            }
        }

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

            string cb;
            string ncb;
            cb = ChatBox.Text;
            ncb = cb + "\r\n" + fromName + ": " + message;
            textBoxUpdate(ChatBox, ncb);

            if (ChatBotCheckbox.Checked && fromName != BotAvatarName)
            {
                String msg = GetBotAnswer(message, fromName);
                if (msg != "")
                {
                    client.Self.Movement.TurnToward(position);
                    client.Self.Chat(msg, chnum, ChatType.Normal);
                }
            }
        }

        public void Self_OnMeanCollision(MeanCollisionType type, UUID perp, UUID victim, float magnitude, DateTime time)
        {
            MessageBox.Show("Bump! " + type.ToString() + ":" + magnitude);
        }

        // Callback for ObjectProperties request received
        void Objects_OnObjectProperties(Simulator simulator, Primitive.ObjectProperties properties)
        {

            lock (PrimsWaiting)
            {
                Primitive prim;
                if (PrimsWaiting.TryGetValue(properties.ObjectID, out prim))
                {
                    prim.Properties = properties;

                    // Dynamically update the objects display

                    string objs = objectsBox.Text + prettyLocation(prim.Position) + "  " + 
                        prettySize(prim.Scale) + " " +
                        properties.Name + " " + properties.Description + "\r\n";
                    textBoxUpdate(objectsBox, objs);
                }
                PrimsWaiting.Remove(properties.ObjectID);

                if (PrimsWaiting.Count == 0)
                    AllPropertiesReceived.Set();
            }
        }


        private void BotControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            if (client != null && client.Network.Connected) client.Network.Logout();
            Environment.Exit(0);
        }

    }
}
