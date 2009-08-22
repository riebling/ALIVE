using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
//using OpenMetaverse;
using ALIVE;

//using HttpServer;
using System.Net;
//using HttpListener = HttpServer.HttpListener;

namespace MyBot
{

    public partial class BotControlForm1 : Form
    {
        private static bool LoginSuccess = false;
        private ALIVE.SmartDog myAvatar;

        //Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

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

            QuitButton.Click += new EventHandler(QuitButton_Click);
            MoveButton.Click += new EventHandler(MoveButton_Click);

            LookButton.Click += new EventHandler(LookButton_Click);
            //ObjectPropsButton.Click += new EventHandler(ObjectPropsButton_Click);
            dropobjectbutton.Click += new EventHandler(dropobjectbutton_Click);
            takeobjectbutton.Click += new EventHandler(takeobjectbutton_Click);

            turnleftbutton.Click += new EventHandler(turnleftbutton_Click);
            turnrightbutton.Click += new EventHandler(turnrightbutton_Click);
            goforwardbutton.Click += new EventHandler(goforwardbutton_Click);
            gobackwardbutton.Click += new EventHandler(gobackwardbutton_Click);

            readchatbutton.Click += new EventHandler(readchatbutton_Click);
            readmessagebutton.Click += new EventHandler(readmessagebutton_Click);
            saychatbutton.Click += new EventHandler(saychatbutton_Click);
            saymessagebutton.Click += new EventHandler(saymessagebutton_Click);

            turntowardbutton.Click += new EventHandler(turntowardbutton_Click);
            mycoordinatesbutton.Click +=new EventHandler(mycoordinatesbutton_Click);
            myrotationbutton.Click += new EventHandler(rotationbutton_Click);

            //// Register callback to catch Object properties events
            //client.Objects.OnObjectProperties += new ObjectManager.ObjectPropertiesCallback(Objects_OnObjectProperties);

            //force logout and exit when form is closed
            this.FormClosing += new FormClosingEventHandler(BotControl_FormClosing);
        }



/////// SUBROUTINES


        void displayMyLocation()
        {
            //string myLocation = prettyLocation(client.Self.SimPosition);
            float x, y;
            myAvatar.Coordinates(out x, out y);
            textBoxUpdate(locationBox, "<" + x.ToString("0.0") + "," + y.ToString("0.0") + ">");
        }

        // Just for fun: Connect to a Pandora chatbot (ALICE)
        // uses fromName as a unique ID for Pandora to 'remember'
        // the conversation thread
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

            // Delay 7 seconds to simulate conversational pause
            Thread.Sleep(7000);

            // log and return response
            //Console.WriteLine(msglines);
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

                myAvatar = new ALIVE.SmartDog(FNtextBox.Text, LNtextBox.Text, PWtextBox.Text, "SNOOPY");
                LoginSuccess = myAvatar.Login();
                //client.Network.CurrentSim.ObjectsAvatars.ForEach();
                //client.Network.CurrentSim.ObjectsPrimitives.ForEach();
                
                if (LoginSuccess)
                {
                    //client.Settings.DISABLE_AGENT_UPDATE_DUPLICATE_CHECK = true;

                    QuitButton.Text = "Logout";
                    ChatBox.Visible = true;
                    InputBox.Visible = true;
                    saychatbutton.Visible = true;
                    PWtextBox.Text = "";
                    }
                else
                {
                    //MessageBox.Show("Din't work! " + client.Network.LoginMessage);
                    return;
                }

                displayMyLocation();
            }
            else
            {
                //client.Network.Logout();
                myAvatar.Logout();
                QuitButton.Text = "Login";
                ChatBox.Visible = false;
                InputBox.Visible = false;
                saychatbutton.Visible = false;

                objectsBox.Text = "";
                locationBox.Text = "";
                }
        }


        void LookButton_Click(object sender, EventArgs e)
        {
            textBoxUpdate(objectsBox, "");
            LookButton.Enabled = false;

            List<AliveObject> prims = myAvatar.ObjectsAround();
            
            //Console.Out.WriteLine("Got " + prims.Count + " objects back");

            // Synchronous object info update
            String message ="";
            for (int i = 0; i < prims.Count; i++)
                message += prims[i].toString() + "\r\n";

            textBoxUpdate(objectsBox, message);

            LookButton.Enabled = true;
        }

        //void ObjectPropsButton_Click(object sender, EventArgs e)
        //{
        //    textBoxUpdate(objectsBox, "");
        //    ObjectPropsButton.Enabled = false;

        //    List<AliveObject> prims = myAvatar.ObjectsAround();

        //    string message = "";
            
        //    for (int i = 0; i < prims.Count; i++)
        //    {
        //        string submessage = "";
        //        AliveObject p = prims[i];
        //        List<string> props = myAvatar.GetObjectProps(p);
        //        for (int j = 0; j < props.Count; j++)
        //            //submessage = submessage + props[j] + " ";
        //            submessage = submessage + props[j] + "=" + myAvatar.GetObjProp(p, props[j]) + " ";
        //        message += submessage + "\r\n";
        //    }

        //    textBoxUpdate(objectsBox, message);

        //    ObjectPropsButton.Enabled = true;
        //}

        void MoveButton_Click(object sender, EventArgs e)
        {
            if (Xbox.Text == "" || Ybox.Text == "") return;
            Boolean success = myAvatar.GoTo(Convert.ToInt32(Xbox.Text), Convert.ToInt32(Ybox.Text));
        }

        private void rotationbutton_Click(object Sender, EventArgs e)
        {
            float rot = myAvatar.Orientation();
            textBoxUpdate(rotationBox, rot.ToString());
        }

        private void turnleftbutton_Click(object sender, EventArgs e)
        {
            if (turnleftbox.Text == "") return;
            myAvatar.TurnLeft(Convert.ToInt32(turnleftbox.Text));
        }
        private void turnrightbutton_Click(object sender, EventArgs e)
        {
            if (turnrightbox.Text == "") return;
            myAvatar.TurnRight(Convert.ToInt32(turnrightbox.Text));
        }
        void turntowardbutton_Click(object sender, EventArgs e)
        {
            if (Xbox.Text == "" || Ybox.Text == "") return;
            myAvatar.TurnTo(Convert.ToInt32(Xbox.Text), Convert.ToInt32(Ybox.Text));
        }

        private void goforwardbutton_Click(object sender, EventArgs e)
        {
            if (goforwardbox.Text == "") return;
            myAvatar.GoForward(Convert.ToInt32(goforwardbox.Text));
        }

        private void gobackwardbutton_Click(object sender, EventArgs e)
        {
            if (gobackwardbox.Text == "") return;
            myAvatar.GoBackward(Convert.ToInt32(gobackwardbox.Text));
        }

        private void mycoordinatesbutton_Click(object sender, EventArgs e)
        {
            //string myLocation = prettyLocation(client.Self.SimPosition);
            float x, y;
            myAvatar.Coordinates(out x, out y);
            textBoxUpdate(locationBox, "<" + x.ToString("0.0") + "," + y.ToString("0.0") + ">");

        }

        private void readchatbutton_Click(object sender, EventArgs e)
        {
            chatboxlabel.Text = "chat";
            textBoxUpdate(ChatBox, myAvatar.GetChat());
        }

        private void readmessagebutton_Click(object sender, EventArgs e)
        {
            chatboxlabel.Text = "message";
            textBoxUpdate(ChatBox, myAvatar.GetMessage());
        }

        private void saychatbutton_Click(object sender, EventArgs e)
        {
            myAvatar.SayChat(InputBox.Text);
            InputBox.Text = "";
        }

        private void saymessagebutton_Click(object sender, EventArgs e)
        {
            myAvatar.SayMessage(InputBox.Text);
            InputBox.Text = "";
        }

        private void dropobjectbutton_Click(object sender, EventArgs e)
        {

            //myAvatar.DropObject(Convert.ToUInt32(objectidbox.Text));
        }

        private void takeobjectbutton_Click(object sender, EventArgs e)
        {
            //myAvatar.PickupObject(Convert.ToUInt32(objectidbox.Text));
        }




////  CALLBACKS

        private void BotControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            if (myAvatar != null)
                myAvatar.Logout();
            Environment.Exit(0);
        }


    }
}
