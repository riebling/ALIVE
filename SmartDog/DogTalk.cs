using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using ALIVE;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Collections.ObjectModel;
using SimpleGrammar;


namespace DogsBrain
{
    public static class DogTalk
    {
        public static void obeyText(DogsMind myMind, string sentence)
        {
            if (sentence == null || sentence == "")
            {
                Console.WriteLine("obeyText: sentence empty");
                myMind.mySynth.SpeakAsync("Sorry, master, I didn't get this command");
                return;
            }
            ParseTree pt = new ParseTree(myMind.ParseMachine, sentence); //This calls the parser
            if (pt == null || pt.root == null)
            {
                myMind.form.objectBoxUpdate("Parse failed");
                Console.WriteLine("Parse of failed");
                return;
            }
            pt.root.merge_W_leaves(); //The parser breaks up the phrases into individual words, this call restores the phrases from the grammar
            Console.WriteLine("Parse result: " + pt.root.toSexp());
            CD commandCD = new CD(pt.root); // this call builds the CD
            Console.WriteLine("Command: " + commandCD.ToSexp());
            concept comCon = commandCD.head;
            string cmd = comCon.concept_name;
            if (dogTask.tasks.Contains(cmd)) //If we got a known command, create a new dog task
            {
                //Save the command arguments in the fields of the new dog task and invoke the command method
                dogTask dt = new dogTask(myMind, cmd);
                dt.taskArgs = commandCD;
                myMind.myContext.dt = dt;
                myMind.myContext.last_command = commandCD;
                myMind.myContext.last_cmd = cmd;
                float x, y;
                myMind.myDog.Coordinates(out x, out y);
                myMind.myContext.start_pos_x = x;
                myMind.myContext.start_pos_y = y;
                myMind.myContext.start_angle = myMind.myDog.Orientation();
                myMind.dogThread = new Thread(DogTalk.doTaskThread);
                myMind.dogThread.Start(myMind);
            }
            else
            {
                Console.WriteLine("Unknown command: " + cmd);
                myMind.mySynth.SpeakAsync("Sorry, master, I don't know this command");
            }
        }

        public static void doTaskThread(object data)
        {
            DogsMind myMind = (DogsMind)data;
            dogTask dt = myMind.myContext.dt;
            string cmd = myMind.myContext.last_cmd;
            Type myType = dt.GetType();
            MethodInfo myInfo = myType.GetMethod(cmd);
            myInfo.Invoke(dt, null);
            myMind.myDog.PlayAnimation(AliveAnimation.HELLO);
            myMind.mySynth.SpeakAsync("Done, master!");
        }

        //This is the old code to obey text commands through the chat box
        public static void obeyMaster(DogsMind dm)
        {
            ALIVE.SmartDog myDog = dm.myDog;
            string msg = "";
            dogTask dt;
            myDog.SayMessage("I am ready");
            msg = myDog.GetMessage();
            if (msg == null || msg == "") msg = myDog.GetMessage();
            if (msg == null || msg == "")
            {
                myDog.SayMessage("Wooff, wooff!");
                return;
            }
            int ind = msg.IndexOf(":");
            string cmd = msg.Substring(ind + 2);
            int length = cmd.Length;
            cmd = cmd.Substring(0, length - 2);
            if (dogTask.tasks.Contains(cmd))
            {
                dt = new dogTask(dm, cmd);
                Type myType = dt.GetType();
                MethodInfo myInfo = myType.GetMethod(cmd);
                myInfo.Invoke(dt, null);
                myDog.SayMessage("Done, master!");
                return;
            }
            myDog.SayMessage("Wooff, wooff!");
        }

    }
}
