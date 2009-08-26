using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;

using ALIVE;


namespace MyBot
{

    public partial class BotControlForm1 : Form
    {
        private static bool LoginSuccess = false;
        private ALIVE.SmartDog myAvatar;
        private List<AliveObject> Prims;
        private AliveObject carriedPrim = null;

        //Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        // The delegate and the funny-looking method solve an annoying problem of
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
            AutoButton.Click += new EventHandler(AutoButton_Click);

            LookButton.Click += new EventHandler(LookButton_Click);
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

        
//////  EVENT HANDLERS

        private void BotControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            if (myAvatar != null)
                myAvatar.Logout();
            Environment.Exit(0);
        }

        void QuitButton_Click(object sender, EventArgs e)
        {
            if (QuitButton.Text == "Login")
            {

                myAvatar = new ALIVE.SmartDog(FNtextBox.Text, LNtextBox.Text, PWtextBox.Text, URIbox.Text);
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

            Prims = myAvatar.ObjectsAround();
            
            //Console.Out.WriteLine("Got " + prims.Count + " objects back");

            // Synchronous object info update
            String message ="";
            for (int i = 0; i < Prims.Count; i++)
                message += i + " " + Prims[i].toString() + "\r\n";

            textBoxUpdate(objectsBox, message);

            LookButton.Enabled = true;
        }

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
            if (carriedPrim != null)
            {
                myAvatar.DropObject(carriedPrim);
                carriedPrim = null;
            }
        }

        private void takeobjectbutton_Click(object sender, EventArgs e)
        {
            if (objectidbox.Text != null) // try to guard against empty textbox
            {
                int objectIndex = Convert.ToInt32(objectidbox.Text);
                if (Prims.Count >= objectIndex) // try to guard against bad index
                {
                    if (myAvatar.PickupObject(Prims[objectIndex]))
                        carriedPrim = Prims[objectIndex];
                    else
                        carriedPrim = null;
                }
            }
        }

        private void AutoButton_Click(object sender, EventArgs e)
        {
            // Add code here
        }

    }
}
