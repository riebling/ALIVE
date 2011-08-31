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
using DogsBrain;
using SimpleGrammar;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Collections.ObjectModel;


namespace MyBot
{

    public partial class BotControlForm1 : Form
    {
        private static bool LoginSuccess = false;
        private ALIVE.SmartDog myAvatar;
        private List<AliveObject> Prims;
        private AliveObject carriedPrim = null;
        private SpeechSynthesizer mySynth;
        private SpeechRecognitionEngine myRecog;
        private ReadOnlyCollection<InstalledVoice> voices;
        private DogsBrain.DogsMind myMind;
        private sgrMachine Machine;
        private string[] allFormCommands = 
        { "Go forward","Go backward","Turn left","Turn right","Go to point","Turn toward point",
            "Take object","Drop object","Report objects around","My coordinates","My rotation",
            "Run animation","Read message","Say message","Read chat","Say chat","Speech to text",
            "Text to parse tree","Text to parse tree to CD"};

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

        public delegate void objectsTextBoxUpdater(string nvalue);

        public void objectBoxUpdate(string text)
        {
            if (this.objectsBox.InvokeRequired)
            {
                objectsTextBoxUpdater d = new objectsTextBoxUpdater(objectBoxUpdate);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.objectsBox.Text = text;
            }
        }


        public BotControlForm1()
        {
            InitializeComponent();

            QuitButton.Click += new EventHandler(QuitButton_Click);
            AutoButton.Click += new EventHandler(AutoButton_Click);

            SayButton.Click += new EventHandler(SayButton_Click);
            speedTrackBar.Scroll += new EventHandler(speedTrackBar_Scroll);
            volumeTrackBar.Scroll += new EventHandler(volumeTrackBar_Scroll);

            //force logout and exit when form is closed
            this.FormClosing += new FormClosingEventHandler(BotControl_FormClosing);

            try
            {
                sgrNode gr = SimpleGrammar.MyGr.buildMyGrammar();
                Machine = new sgrMachine(gr);
                concept.initConcepts();
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("Something nasty in the grammar initializers: " + ex.ToString());
            }
        }


/////// SUBROUTINES


        void displayMyLocation()
        {
            //string myLocation = prettyLocation(client.Self.SimPosition);
            float x, y;
            myAvatar.Coordinates(out x, out y);
            textBoxUpdate(objectsBox, "My current location: <" + x.ToString("0.0") + "," + y.ToString("0.0") + ">");
        }

        
//////  EVENT HANDLERS

        private void BotControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            if (myAvatar != null)
                abortDogThread();
                myAvatar.Logout();
            Environment.Exit(0);
        }

        // Login or Logout
        void QuitButton_Click(object sender, EventArgs e)
        {
            if (QuitButton.Text == "Login")
            {
                try
                {
                    myAvatar = new ALIVE.SmartDog(FNtextBox.Text, LNtextBox.Text, PWtextBox.Text, URIbox.Text);
                    //myAvatar.ALIVE_SERVER = "http://OHIO.LTI.CS.CMU.EDU";
                    if (URIbox.Text == "ALIVE-local") myAvatar.ALIVE_SERVER = "http://127.0.0.1:9000";
                    LoginSuccess = myAvatar.Login();
                    if (LoginSuccess) startEngines();
                    else Console.WriteLine("Login failed");
                }
                catch (System.Exception ex)
                {
                    Console.WriteLine("Something very nasty happend in dog initialization: " + ex.ToString());
                }
            }
            else
            {
                myAvatar.Logout();
                stopEngines();
            }
        }

       
        private void startEngines()
        {
            QuitButton.Text = "Logout";
            ChatBox.Visible = true;
            InputBox.Visible = true;
            PWtextBox.Text = "";
            speedTrackBar.Visible = true;
            volumeTrackBar.Visible = true;
            textButton.Visible = true;
            SayButton.Visible = true;
            SayButton.Text = "Voice Command";
            foreach (string c in allFormCommands) commandSelectBox1.Items.Add(c);
            commandSelectBox1.Text = "Select command";
            commandSelectBox1.Visible = true;
            AutoButton.Visible = true;
            commandButton1.Visible = true;
            displayMyLocation();
            mySynth = new SpeechSynthesizer();
            myRecog = new SpeechRecognitionEngine();
            myRecog.SetInputToDefaultAudioDevice();
            myRecog.RecognizeCompleted += new EventHandler<RecognizeCompletedEventArgs>(myRecog_RecognizeCompleted);
            myRecog.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(myRecog_SpeechRecognitionRejected);
            Grammar mymsgr = SimpleGrammar.MyGr.buildMsoftGrammar();
            myRecog.LoadGrammar(mymsgr);
            voices = mySynth.GetInstalledVoices();
            if (voices == null || voices.Count == 0)
            {
                volumeTrackBar.Enabled = false;
                speedTrackBar.Enabled = false;
            }
            else
            {
                selectVoiceComboBox.Visible = true;
                foreach (InstalledVoice v in voices)
                {
                    VoiceInfo vi = v.VoiceInfo;
                    selectVoiceComboBox.Items.Add(vi.Name.ToString());
                }
                selectVoiceComboBox.Text = selectVoiceComboBox.Items[0].ToString();
                mySynth.SelectVoice(selectVoiceComboBox.Text);
            }
            myMind = new DogsMind(myAvatar, mySynth, myRecog, Machine);
            myMind.form = this;
            //myMind.oboxSay += delegate(string x) { objectsBox.Text = x; };
        }

 
        private void stopEngines()
        {
            abortDogThread();
            QuitButton.Text = "Login";
            ChatBox.Visible = false;
            InputBox.Visible = false;
            objectsBox.Text = "";
            speedTrackBar.Visible = false;
            volumeTrackBar.Visible = false;
            SayButton.Visible = false;
            textButton.Visible = false;
            AutoButton.Visible = false;
            commandButton1.Visible = false;
            commandSelectBox1.Items.Clear();
            commandSelectBox1.Text = "Select command";
            commandSelectBox1.Visible = false;
            selectVoiceComboBox.Items.Clear();
            selectVoiceComboBox.Text = "Select voice";
            selectVoiceComboBox.Visible = false;
        }

        private void abortDogThread()
        {
            if (myMind != null && myMind.dogThread != null && myMind.dogThread.IsAlive)
            {
                myAvatar.stopMoving();
                myMind.dogThread.Abort();
            }
        }

        //Puts the dog into the autonomous mode where it only listens to the Master through chat
        private void AutoButton_Click(object sender, EventArgs e)
        {
            // Add code here
            //DogsBrain.dogsLife.obeyMaster(myAvatar);
        }

        //Voice control handlers
        void SayButton_Click(object sender, EventArgs e)
        {
            string status = SayButton.Text;
            try
            {
                if (status == "Voice Command")
                {
                    SayButton.Text = "Done speaking";
                    abortDogThread();
                    myRecog.RecognizeAsync();
                }
                else
                {
                    myRecog.RecognizeAsyncStop();
                    SayButton.Text = "Voice Command";
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("SayButton Nasty: " + ex.ToString());
            }
        }

        void volumeTrackBar_Scroll(object sender, EventArgs e)
        {
            mySynth.Volume = volumeTrackBar.Value;
        }

        void speedTrackBar_Scroll(object sender, EventArgs e)
        {
            mySynth.Rate = speedTrackBar.Value;
        }
        private void myRecog_RecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Result != null)
            {
                if (commandSelectBox1.Text != "Speech to text")
                {
                    DogsBrain.DogTalk.obeyText(myMind, e.Result.Text);
                }
                else
                {
                    objectsBox.Text = e.Result.Text;
                }
            }
            else
            {
                objectsBox.Text = "Speech recognition failed";
            }
        }

        void myRecog_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            Console.WriteLine("Speech rejected: " + e.ToString());
        }

        private void textButton_Click(object sender, EventArgs e)
        {
            if (InputBox.Text == "") return;
            abortDogThread();
            DogsBrain.DogTalk.obeyText(myMind, InputBox.Text);
        }


        private void commandButton1_Click(object sender, EventArgs e)
        {
            abortDogThread();
            string command = commandSelectBox1.Text;
            objectsBox.Text = "";
            try
            {
                switch (command)
                {
                    case "Go forward":
                        goForward();
                        break;
                    case "Go backward":
                        goBackward();
                        break;
                    case "Turn left":
                        turnLeft();
                        break;
                    case "Turn right":
                        turnRight();
                        break;
                    case "Go to point":
                        goToPoint();
                        break;
                    case "Turn toward point":
                        turnTowardPoint();
                        break;
                    case "Take object":
                        takeObject();
                        break;
                    case "Drop object":
                        dropObject();
                        break;
                    case "Report objects around":
                        reportObjectAround();
                        break;
                    case "My coordinates":
                        myCoordiantes();
                        break;
                    case "My rotation":
                        myRotation();
                        break;
                    case "Run animation":
                        runAnimation();
                        break;
                    case "Read message":
                        readMessage();
                        break;
                    case "Say message":
                        sayMessage();
                        break;
                    case "Read chat":
                        readChat();
                        break;
                    case "Say chat":
                        sayChat();
                        break;
                    case "Speech to text":
                        speechToText();
                        break;
                    case "Text to parse tree":
                        textToParseTree();
                        break;
                    case "Text to parse tree to CD":
                        textToParseTreeToCD();
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("Something very nasty happend in command execution: " + ex.ToString());
            }
        }

        private void goForward()
        {
            if (InputBox.Text == "") return;
            myAvatar.GoForward(Convert.ToInt32(InputBox.Text));
        }

        private void goBackward()
        {
            if (InputBox.Text == "") return;
            myAvatar.GoBackward(Convert.ToInt32(InputBox.Text));
        }

        private void turnLeft()
        {
            if (InputBox.Text == "") return;
            myAvatar.TurnLeft(Convert.ToInt32(InputBox.Text));
        }

        private void turnRight()
        {
            if (InputBox.Text == "") return;
            myAvatar.TurnRight(Convert.ToInt32(InputBox.Text));
        }

        private void goToPoint()
        {
            if (Xbox.Text == "" || Ybox.Text == "") return;
            Boolean success = myAvatar.GoTo(Convert.ToInt32(Xbox.Text), Convert.ToInt32(Ybox.Text));
        }

        private void turnTowardPoint()
        {
            if (Xbox.Text == "" || Ybox.Text == "") return;
            myAvatar.TurnTo(Convert.ToInt32(Xbox.Text), Convert.ToInt32(Ybox.Text), 26);
        }

        private void takeObject()
        {
            if (InputBox.Text != null) // try to guard against empty textbox
            {
                int objectIndex = Convert.ToInt32(InputBox.Text);
                if (Prims.Count >= objectIndex) // try to guard against bad index
                {
                    if (myAvatar.PickupObject(Prims[objectIndex]))
                        carriedPrim = Prims[objectIndex];
                    else
                        carriedPrim = null;
                }
            }
        }

        private void dropObject()
        {
            if (carriedPrim != null)
            {
                myAvatar.DropObject(carriedPrim);
                carriedPrim = null;
            }
        }

        private void reportObjectAround()
        {
            string message = "";
            Prims = myAvatar.ObjectsAround();
            for (int i = 0; i < Prims.Count; i++)
                message += i + " " + Prims[i].toString() + "\r\n";
            objectBoxUpdate(message);
        }

        private void myCoordiantes()
        {
            float x, y;
            myAvatar.Coordinates(out x, out y);
            objectBoxUpdate("<" + x.ToString("0.0") + "," + y.ToString("0.0") + ">");
        }

        private void myRotation()
        {
            float rot = myAvatar.Orientation();
            objectBoxUpdate(rot.ToString());
        }

        private void runAnimation()
        {
            switch (InputBox.Text)
            {
                case "Hello":
                    myAvatar.PlayAnimation(AliveAnimation.HELLO);
                    break;
                case "Point":
                    myAvatar.PlayAnimation(AliveAnimation.POINT_YOU);
                    break;
                default:
                    break;
            }
        }

        private void readMessage()
        {
            chatboxlabel.Text = "message";
            textBoxUpdate(ChatBox, myAvatar.GetMessage());
        }

        private void sayMessage()
        {
            myAvatar.SayMessage(InputBox.Text);
            InputBox.Text = "";
        }

        private void readChat()
        {
            chatboxlabel.Text = "chat";
            textBoxUpdate(ChatBox, myAvatar.GetChat());
        }

        private void sayChat()
        {
            myAvatar.SayChat(InputBox.Text);
            InputBox.Text = "";
        }

        private void speechToText()
        {
            throw new NotImplementedException();
        }

        private void textToParseTree()
        {
            ParseTree pt = new ParseTree(Machine, InputBox.Text);
            if (pt == null || pt.root == null) objectsBox.Text = "Parse failed";
            else
            {
                pt.root.merge_W_leaves();
                string message = "Parse Tree:" + Environment.NewLine + pt.root.toSexp();
                objectsBox.Text = message;
            }
        }

        private void textToParseTreeToCD()
        {
            ParseTree pt = new ParseTree(Machine, InputBox.Text);
            if (pt == null || pt.root == null) objectsBox.Text = "Parse failed";
            else
            {
                pt.root.merge_W_leaves();
                CD res = new CD(pt.root);
                string message = "Parse Tree:" + Environment.NewLine + pt.root.toSexp() + Environment.NewLine;
                message += "Conceptual Description (CD):" + Environment.NewLine + res.ToSexp();
                objectsBox.Text = message;
            }
        }

        private void commandSelectBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            commandSelectBox1.Text = (string)commandSelectBox1.SelectedItem;
        }

        private void selectVoiceComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectVoiceComboBox.Text = (string)selectVoiceComboBox.SelectedItem;
            mySynth.SelectVoice(selectVoiceComboBox.Text);
        }

    }
}
