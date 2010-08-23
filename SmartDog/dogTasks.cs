using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ALIVE;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Collections.ObjectModel;

namespace DogsBrain
{
    public class dogTask
    {
        public string task_name;
        public DogsMind myMind;
        public CD taskArgs;
        public ArrayList target_objects;
        public static List<string> tasks = new List<string> { "turn", "turn_to_object", "turn_around", "go", "go_to_object", "go_to_center", "report", "pick_up_object", "drop"};
        public dogTask(DogsMind dm, string t_name)
        {
            task_name = t_name;
            myMind = dm;
            target_objects = new ArrayList();
            dm.myDog.Coordinates(out dm.myContext.start_pos_x, out dm.myContext.start_pos_y);
            dm.myContext.start_angle = dm.myDog.Orientation();
        }

        public void turn()
        {
            Hashtable args = taskArgs.PropList;
            string turn_dir = "left";
            if (args["turn_direction:"] != null) turn_dir = (string)args["turn_direction:"];
            int turn_deg = 90;
            if (args["num:"] != null) turn_deg = (int)args["num:"];
            int rot = (int)myMind.myDog.Orientation();
            switch (turn_dir)
            {
                case "left":
                    myMind.myDog.TurnLeft(turn_deg);
                    break;
                case "right":
                    myMind.myDog.TurnRight(turn_deg);
                    break;
                case "around":
                    myMind.myDog.TurnLeft(180);
                    break;
                case "north": 
                    myMind.myDog.TurnLeft(rot);
                    break;
                case "east":
                    myMind.myDog.TurnLeft(rot);
                    myMind.myDog.TurnRight(90);
                    break;
                case "west":
                    myMind.myDog.TurnLeft(rot);
                    myMind.myDog.TurnLeft(90);
                    break;
                case "south":
                    myMind.myDog.TurnLeft(rot);
                    myMind.myDog.TurnLeft(180);
                    break;
            }
        }

        public void turn_to_object()
        {
            CD target_CD = (CD)taskArgs.PropList["object:"];
            AliveObject target;
            float conf = dogTricks.selectKnowObject(myMind, target_CD, out target);
            if (conf > .5F)
            {
                myMind.myDog.TurnTo(target.X, target.Y);
                myMind.myContext.last_focus = target;
                return;
            }
            myMind.mySynth.SpeakAsync("I don't see the right target");
        }

        public void turn_around()
        {
            myMind.myDog.TurnLeft(180);
        }

        public void go()
        {
            Hashtable args = taskArgs.PropList;
            string go_dir = "forward";
            if (args["go_direction:"] != null) go_dir = (string)args["go_direction:"];
            int go_dist = 5;
            if (args["num:"] != null) go_dist = (int)args["num:"];
            if (go_dir == "forward")
            {
                myMind.myDog.GoForward(go_dist);
            }
            else
            {
                myMind.myDog.GoBackward(go_dist);
            }
            myMind.update_explored();
        }

        public void go_to_object()
        {
            CD target_CD = (CD)taskArgs.PropList["object:"];
            AliveObject target;
            float conf = dogTricks.selectKnowObject(myMind, target_CD, out target);
            if (conf > .5F)
            {
                float xx = (float)target.X;
                float yy = (float)target.Y;
                dogTricks.walk_to_point(myMind, target);
                myMind.myDog.TurnTo(xx, yy);
                myMind.myDog.PlayAnimation(AliveAnimation.POINT_YOU);
                myMind.myContext.last_focus = target;
                myMind.update_explored();
                return;
            }
            myMind.mySynth.SpeakAsync("I don't see the right target");
        }

        public void go_to_center()
        {
            dogTricks.walk_to_point(myMind, 128, 128);
        }

        public void report()
        {
            string message = "";
            myMind.update_explored();
            foreach (DictionaryEntry i in myMind.knownObjects)
            {
                string fam = (string)i.Key;
                Hashtable famObjs = (Hashtable)i.Value;
                if (famObjs == null) break;
                int c = famObjs.Count;
                if (c == 0) break;
                string plural = "";
                if (c > 1) plural = "s";
                message = message + "I see " + c.ToString() + " " + fam + plural + "\r\n";
                foreach (DictionaryEntry j in famObjs)
                {
                    AliveObject obj = (AliveObject)j.Value;
                    string name = obj.name;
                    string x = obj.X.ToString();
                    string y = obj.Y.ToString();
                    string color = obj.color;
                    string height = obj.height.ToString();
                    string width = obj.width.ToString();
                    message = message + "   " + name + " " + color + " at (" + x + " , " + y + ")  height = " + height + " width = " + width + "\r\n";
                }
            }
            myMind.oboxSay(message);
        }

        public void pick_up_object()
        {
            CD target_CD = (CD)taskArgs.PropList["object:"];
            AliveObject target;
            if (myMind.myDog.carriedObject != null)
            {
                myMind.mySynth.SpeakAsync("Sorry master, my hands are full");
                return;
            }
            float conf = dogTricks.selectKnowObject(myMind, target_CD, out target);
            if (conf > .3F)
            {
                if (target.movable == false)
                {
                    myMind.mySynth.SpeakAsync("Sorry master, the target is not movable");
                    return;
                }
                float xx = (float)target.X;
                float yy = (float)target.Y;
                dogTricks.walk_to_point(myMind, target);
                myMind.myDog.TurnTo(xx, yy);
                if (myMind.myDog.PickupObject(target) == false)
                {
                    myMind.myDog.carriedObject = null;
                    myMind.mySynth.SpeakAsync("Sorry master, I tried but I cannot pick up the target");
                }
                myMind.myDog.carriedObject = target;
                return;
            }
            myMind.mySynth.SpeakAsync("I don't see the right target");
        }

        public void drop()
        {
            AliveObject target = myMind.myDog.carriedObject;
            if (target == null) myMind.mySynth.SpeakAsync("I am not carrying anything");
            else
            {
                myMind.myDog.DropObject(target);
                myMind.myDog.carriedObject = null;
                myMind.update_explored();
            }
        }
    }

}
