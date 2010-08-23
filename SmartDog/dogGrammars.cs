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
    public static class dogGrammars
    {
        public static void buildDogGrammars(DogsMind myMind)
        {
            SpeechRecognitionEngine myRecog = myMind.myRecog;
            myRecog.LoadGrammar(turnGr());
            myRecog.LoadGrammar(turnToGr());
            myRecog.LoadGrammar(goGr());
            myRecog.LoadGrammar(goToGr());
            myRecog.LoadGrammar(goToCenter());
            myRecog.LoadGrammar(findObj());
            myRecog.LoadGrammar(reportGr());
            myRecog.LoadGrammar(pickGR());
            myRecog.LoadGrammar(dropGr());
        }

//Grammars
        private static Grammar reportGr()
        {
            GrammarBuilder rep = new SemanticResultKey("head_command:", "report");
            Grammar gr = new Grammar(rep);
            gr.Name = "Report grammar";
            return gr;
        }

        
        private static Grammar turnGr()
        {
            Choices turn1Choices = new Choices("left", "right", "around");
            GrammarBuilder turn1Commands = new SemanticResultKey("head_command:", "turn");
            turn1Commands.Append(new SemanticResultKey("turn_direction:", turn1Choices));
            GrammarBuilder turn_degr = new GrammarBuilder();
            turn_degr.Append(numBuilder(""));
            turn_degr.Append("degrees");
            turn1Commands.Append(turn_degr, 0, 1);
            Grammar gr = new Grammar(turn1Commands);
            gr.Name = "Turn right/left x degrees grammar";
            return gr;
        }

        private static Grammar turnToGr()
        {
            GrammarBuilder turn2Commands = new SemanticResultKey("head_command:", new SemanticResultValue("turn to", "turn_to_object"));
            turn2Commands.Append(new SemanticResultKey("object:", new SemanticResultValue(objBuilder(""), true)));
            Grammar gr = new Grammar(turn2Commands);
            gr.Name = "Turn to grammar";
            return gr;
        }

        private static Grammar goGr()
        {
            Choices go1Choices = new Choices("forward", "backward");
            GrammarBuilder go1Commands = new SemanticResultKey("head_command:", "go");
            go1Commands.Append(new SemanticResultKey("go_direction:", go1Choices));
            GrammarBuilder go_meters = new GrammarBuilder();
            go_meters.Append(numBuilder(""));
            go_meters.Append("meters");
            go1Commands.Append(go_meters, 0, 1);
            Grammar gr = new Grammar(go1Commands);
            gr.Name = "Go forward/backward x meters grammar";
            return gr;
        }

        private static Grammar goToGr()
        {
            GrammarBuilder go2Commands = new SemanticResultKey("head_command:", new SemanticResultValue("go to", "go_to_object"));
            go2Commands.Append(new SemanticResultKey("object:", new SemanticResultValue(objBuilder(""), true)));
            Grammar gr = new Grammar(go2Commands);
            gr.Name = "Go to grammar";
            return gr;
        }

        private static Grammar goToCenter()
        {
            GrammarBuilder go2Commands = new SemanticResultKey("head_command:", new SemanticResultValue("go to center", "go_to_center"));
            Grammar gr = new Grammar(go2Commands);
            gr.Name = "Go to center grammar";
            return gr;
        }

        private static Grammar pickGR()
        {
            GrammarBuilder pickUpCommand = new SemanticResultKey("head_command:", new SemanticResultValue("pick up", "pick_up"));
            pickUpCommand.Append(new SemanticResultKey("object:", new SemanticResultValue(objBuilder(""), true)));
            GrammarBuilder pickItUpGB = new SemanticResultKey("head_command:", new SemanticResultValue("pick it up", "pick_it_up"));
            Grammar gr = new Grammar(new Choices(pickItUpGB, pickUpCommand));
            gr.Name = "Pick up grammar";
            return gr;
        }

        private static Grammar dropGr()
        {
            GrammarBuilder drop = new SemanticResultKey("head_command:", new SemanticResultValue("drop it", "drop_it"));
            Grammar gr = new Grammar(drop);
            gr.Name = "Drop it grammar";
            return gr;
        }

        private static Grammar findObj()
        {
            GrammarBuilder findCommands = new SemanticResultKey("head_command:", new SemanticResultValue("find", "find_object"));
            findCommands.Append(new SemanticResultKey("object:", new SemanticResultValue(objBuilder(""), true)));
            Grammar gr = new Grammar(findCommands);
            gr.Name = "Find object grammar";
            return gr;
        }

        private static Grammar bringObj()
        {
            GrammarBuilder bringCommands = new SemanticResultKey("head_command:", new SemanticResultValue("bring", "bring_object"));
            bringCommands.Append(new SemanticResultKey("object:", new SemanticResultValue(objBuilder("obj"), true)));
            bringCommands.Append(new GrammarBuilder("to"));
            bringCommands.Append(new SemanticResultKey("destination", new SemanticResultValue(objBuilder("dest"), true)));
            Grammar gr = new Grammar(bringCommands);
            gr.Name = "Bring object grammar";
            return gr;
        }

//Constituent Grammar Builders
        private static GrammarBuilder numBuilder(string ext)
        {
            string[] num_names = { "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "twenty", "thirty", "forty five", "ninety" };
            int[] num_values = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 20, 30, 45, 90 };
            int k = num_names.Length;
            Choices nums = new Choices();
            for (int i = 0; i < k; i++)
            {
                GrammarBuilder y = new GrammarBuilder(num_names[i]);
                nums.Add(new SemanticResultValue(y, num_values[i]));
            }
            GrammarBuilder res = new GrammarBuilder();
            res.Append(new SemanticResultKey("num:"+ext, nums));
            return res;
        }

        private static GrammarBuilder npBuilder(string key)
        {
            Choices art = new Choices("a", "the");
            Choices adj1 = new Choices("big", "small", "tall", "red", "yellow", "green", "blue", "purple");
            Choices adj2 = new Choices("big", "small", "tall", "red", "yellow", "green", "blue", "purple");
            Choices noun = new Choices("house", "ball", "tree", "wall", "cube");
            noun.Add(new SemanticResultValue("box", "cube"));

            string det_key = "det:" + key;
            string mod1_key = "mod:1" + key;
            string mod2_key = "mod:2" + key;
            string head_key = "head_noun:" + key;

            GrammarBuilder obj = new GrammarBuilder();
            obj.Append(new SemanticResultKey(det_key, art), 0, 1);
            obj.Append(new SemanticResultKey(mod1_key, adj1), 0, 1);
            obj.Append(new SemanticResultKey(mod2_key, adj2), 0, 1);
            obj.Append(new SemanticResultKey(head_key, noun));

            return obj;
        }

        //Object is either a pronoun or a noun phrase followed by up to 2
        //optional post-nominal specifiers

        private static GrammarBuilder objBuilder(string key)
        {
            GrammarBuilder obj = new GrammarBuilder();
            obj.Append(npBuilder(key));
            string pns1_key = "pns:1" + key;
            string pns2_key = "pns:2" + key;
            GrammarBuilder pns1 = buildPns(pns1_key);
            obj.Append(new SemanticResultKey(pns1_key, new SemanticResultValue(pns1, true)), 0, 1);
            GrammarBuilder pns2 = buildPns(pns2_key);
            obj.Append(new SemanticResultKey(pns2_key, new SemanticResultValue(pns2, true)), 0, 1);

            string head_key = "head_pronoun:" + key;
            Choices pronoun = new Choices("me", "you", "it");
            GrammarBuilder pronounGB = new SemanticResultKey(head_key, pronoun);
            GrammarBuilder res = new GrammarBuilder();
            res.Append(new Choices(obj,pronounGB));
            return res;
        }

        //We handle two kinds of post-nominal specifiers:
        //with an object (e.g., "near a tree) and without (e.g., "to the east")
        //The object of the second post-nominal specifier is assumed to be "you"
        //This fragment of the grammar should produce a sequence of two semantic
        //key-value pairs:
        //con: "north" and obj: "you" or an object spec
        private static GrammarBuilder buildPns(string key)
        {
            return new Choices(buildRelToObj(key), buildRelDir(key));
        }


        private static GrammarBuilder buildRelToObj(string key)
        {
            string ck = "rel:1" + key;
            string ok = "obj:" + key;
            Choices conj = new Choices();
            conj.Add(new SemanticResultValue("near","near"));
            conj.Add(new SemanticResultValue("behind", "behind"));
            conj.Add(new SemanticResultValue("in front of", "front"));
            conj.Add(new SemanticResultValue("to the left of", "left"));
            conj.Add(new SemanticResultValue("to the right of", "right"));
            conj.Add(new SemanticResultValue("to the north of", "north"));
            conj.Add(new SemanticResultValue("to the south of", "south"));
            conj.Add(new SemanticResultValue("to the east of", "east"));
            conj.Add(new SemanticResultValue("to the west of", "west"));
            conj.Add(new SemanticResultValue("east of", "east"));
            conj.Add(new SemanticResultValue("west of", "west"));
            conj.Add(new SemanticResultValue("north of", "north"));
            conj.Add(new SemanticResultValue("south of", "south"));

            GrammarBuilder youGB = new SemanticResultKey("head_pronoun" + ok, "you");
            Choices obj = new Choices(npBuilder(ok),youGB);
            GrammarBuilder obj_spec = new SemanticResultKey(ok, new SemanticResultValue(obj, true));

            GrammarBuilder res = new GrammarBuilder();
            res.Append(new SemanticResultKey(ck,conj));
            res.Append(obj_spec);
            return res;
        }

        private static GrammarBuilder buildRelDir(string key)
        {
            string ck = "rel:2" + key;

            Choices conj = new Choices();
            conj.Add(new SemanticResultValue("nearby", "near"));
            conj.Add(new SemanticResultValue("to the left", "left"));
            //conj.Add(new SemanticResultValue("to the right", "right"));
            //conj.Add(new SemanticResultValue("to the north", "north"));
            //conj.Add(new SemanticResultValue("to the south", "south"));
            //conj.Add(new SemanticResultValue("to the east", "east"));
            //conj.Add(new SemanticResultValue("to the west", "west"));
            //conj.Add(new SemanticResultValue("on your left", "left"));
            //conj.Add(new SemanticResultValue("on your right", "right"));
            return (GrammarBuilder)new SemanticResultKey(ck, conj);
        }

    }

}
