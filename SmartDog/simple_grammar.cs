using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Speech;
using System.Speech.Recognition;
using System.Speech.Synthesis;

namespace SimpleGrammar
{

    // Builds a specific simple grammar
    public static class MyGr
    {
        public static Hashtable rules;

        //Rule structure: first argument = name, second argument = rule type: "|" means one of, "+" means sequence
        //[n]xyz means that xyz is repeated between 0 and n times, 1<=n<=5, no spaces allowed inside xyz
        //no recursion is allowed - this is a finite grammar
        // Some of the rule names begin with the interpreter instructions such as "cd>", "props>", "pvp", and "head>"
        // The parser pays no attention to these instructions, only the interpreter does
        // The interpreter will build a conceptual description for every node whose name starts with 'cd>"
        // "pvp>" instructs the interpreter to build a property name ("propn>") property value "cd>") pair
        // "skip>" instructs the interpreter to treat the children of the node on the same level as the current node
        // A rule whose name starts with "head>" can only be an "|" rule with strings
        // It will be interpreted as the head concept of a conceptual description
        // All other non-terminals in a "+" rule will be interpreted as the names of properties
        public static sgrNode buildMyGrammar()
        {
            rule("S", "|", "cd>SIMPLE_COMMAND", "cd>COMMAND+OBJECT", "cd>GO+DIR+DIST", "cd>TURN+DIR+DEGREES");
            rule("cd>SIMPLE_COMMAND", "|", "turn around", "drop", "go to center", "report");
            rule("cd>COMMAND+OBJECT", "+", "head>COMMAND2", "cd>object:");
            rule("head>COMMAND2", "|", "go to", "turn to", "pick up");
            rule("cd>object:", "|", "cd>ME", "cd>OBJECT_DESCRIPTION");
            rule("cd>ME", "|", "me");
            rule("cd>OBJECT_DESCRIPTION", "+", "[1]art:", "[1]size:", "[1]color:", "head>NOUN", "[1]skip>location:");
            rule("art:", "|", "a", "the");
            rule("size:", "|", "big", "small", "tall");
            rule("color:", "|", "red", "blue", "yellow", "green", "purple");
            rule("head>NOUN", "|", "ball", "tree", "box", "cube", "house", "wall", "pine");
            rule("skip>location:", "|", "LOC_SPEC", "pvp>LOC+REF_OBJECT");
            rule("LOC_SPEC", "|", "on your right", "on your left", "to the east", "to the west", "to the north", "to the south");
            rule("pvp>LOC+REF_OBJECT", "+", "propn>spec:", "cd>ref:");
            rule("propn>spec:", "|", "near", "behind", "to the left of", "to the right of", "in front of", "east of", "west of", "north of", "south of");
            rule("cd>ref:", "|", "you", "cd>SIMPLE_OBJECT_DESCRIPTION");
            rule("cd>SIMPLE_OBJECT_DESCRIPTION", "+", "[1]art:", "[1]size:", "[1]color:", "head>NOUN");
            rule("cd>GO+DIR+DIST", "+", "head>GO", "go_direction:", "[1]skip>GO_DIST");
            rule("head>GO", "|", "go");
            rule("go_direction:", "|", "forward", "backward");
            rule("skip>GO_DIST", "+", "num:", "meters");
            rule("cd>TURN+DIR+DEGREES", "+", "head>TURN", "turn_direction:", "[1]skip>TURN_DEG");
            rule("head>TURN", "|", "turn");
            rule("turn_direction:", "|", "left", "right", "east", "west", "north", "south");
            rule("skip>TURN_DEG", "+", "num:", "degrees");
            rule("num:", "|", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "twenty", "thirty", "forty five", "sixty", "ninety");
            sgrNode res = new sgrNode(rules, "S");
            return res;
        }

        public static void rule(params string[] args)
        {
            if (rules == null) rules = new Hashtable();
            string rule_name = args[0];
            sgrRule r = new sgrRule(args);
            rules.Add(rule_name, r);
        }

        //builds a Microsoft Speech Recognition Engine grammar from Simple Grammar (sgr) rules
        //Note, that for the sgr parser, we split terminal phrases such as "to the right of" into individual words,
        //we don't do this for the speech grammar
        public static Grammar buildMsoftGrammar()
        {
            if (rules == null) throw new Exception("buildMsoftGrammar: Rules are empty");
            GrammarBuilder S = makeGrBuilder("S");
            Grammar result = new Grammar(S);
            return result;
        }

        private static GrammarBuilder makeGrBuilder(string rule_name)
        {
            sgrRule rule = (sgrRule)rules[rule_name];
            if (rule == null) return new GrammarBuilder(rule_name);
            if (rule.rule_type == "|") // elements of choices should not be optional or repeated
            {
                Choices res = new Choices();
                foreach (string el in rule.elements) res.Add(makeGrBuilder(el));
                return new GrammarBuilder(res);
            }
            GrammarBuilder result = new GrammarBuilder();
            foreach (string el in rule.elements)
            {
                if (el.StartsWith("[") == true)
                {
                    int n = "12345".IndexOf(el[1]) + 1;
                    string word = el.Substring(3);
                    GrammarBuilder child = makeGrBuilder(word);
                    result.Append(new GrammarBuilder(child, 0, n));
                }
                else result.Append(makeGrBuilder(el));
            }
            return result;
        }

    }

    //builds a graph representing a simple grammar
    //It looks like a linear grammar but since we do not allow recursion, the language is finite
    public class sgrNode
    {
        public string node_type;
        public bool optional;
        public string name;
        public ArrayList children;

        public sgrNode()
        {
        }

        public sgrNode(Hashtable rules, string rule_name)
        {
            optional = false;
            name = rule_name;
            sgrRule rule = (sgrRule)rules[rule_name];
            if (rule == null && rule_name.Contains(" ") == false) //terminal
            {
                node_type = "T";
                name = rule_name;
                return;
            }
            if (rule_name.Contains(" ") == true) //then this is a string of non-optonal terminals
            {
                string[] words = rule_name.Split();
                children = new ArrayList();
                name = "W";
                node_type = "+";
                foreach (string w in words)
                {
                    sgrNode child = new sgrNode();
                    child.name = w;
                    child.optional = false;
                    child.node_type = "T";
                    children.Add(child);
                }
                return;
            }
            if (rule.rule_type == "|") // elements of choices should not be optional or repeated
            {
                node_type = "|";
                children = new ArrayList();
                foreach (string el in rule.elements) children.Add(new sgrNode(rules, el));
                return;
            }
            node_type = "+"; //if we got this far, the node is a sequence
            children = new ArrayList();
            foreach (string el in rule.elements)
            {
                if (el.StartsWith("[") == true)
                {
                    int n = "12345".IndexOf(el[1]) + 1;
                    string word = el.Substring(3);
                    for (int i = 1; i <= n; i++)
                    {
                        sgrNode child = new sgrNode(rules, word);
                        child.optional = true;
                        children.Add(child);
                    }
                }
                else children.Add(new sgrNode(rules, el));
            }
        }

    }

    public class sgrRule
    {
        public string name;
        public string rule_type;
        public string[] elements;

        public sgrRule(params string[] args)
        {
            int n = args.Length;
            if (n < 3) throw new Exception("Too few arguments to sgrRule");
            rule_type = args[1];
            if (rule_type != "|" && rule_type != "+") throw new Exception("Bad rule type arg in sgrRule");
            name = args[0];
            elements = new string[n - 2];
            Array.Copy(args, 2, elements, 0, n - 2);
        }
    }

}