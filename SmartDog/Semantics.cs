using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.ObjectModel;
using SimpleGrammar;


namespace DogsBrain
{
    // CD is a conceptual description of an object or action
    public class CD
    {
        public concept head;
        public Hashtable PropList;

        public CD(string str)
        {
            head = (concept)concept.all_concepts[str];
            if (head == null) throw (new System.Exception("New CD: attempt to create from unkown concept: " + str));
            PropList = new Hashtable();
        }

        public CD(SimpleGrammar.ParseNode pn)
        {
            if (pn == null) throw (new Exception("CD null argument"));
            PropList = new Hashtable();
            if (pn.value == "S") // skip the S node, it should have only one child
            {
                if (pn.children == null) throw (new Exception("CD: S node has no children"));
                pn = (ParseNode)pn.children[0];
            }
            if (pn.children == null || pn.children.Count == 0) { make_cd_head(this, pn); return; } // a node without children becomes a CD
            string pn_value = (string)pn.value;
            if (pn_value.StartsWith("cd>") == false) throw (new Exception("CD: the node is not a CD"));
            make_cd(this, pn);
        }

        private void make_cd(CD cd, ParseNode pn)
        {
            // a CD has to have a head concept
            // if the CD parse node has only one child it must be the head
            // otherwise, the interpreter will look for an explicit head among the children of the parse node
            if (pn.children == null || pn.children.Count == 0) throw (new Exception("CD: the node has no children"));
            if (pn.children.Count == 1)
            {
                ParseNode child = (ParseNode)pn.children[0];
                if (child.value.StartsWith("cd>") == true) { make_cd(cd, child); } //descend the chain of cd parse nodes
                else make_cd_head(cd, child);
            }
            else foreach (ParseNode p in pn.children) { make_cd_property(cd, p, ""); };
        }

        private void make_cd_head(CD cd, ParseNode pn)
        {
            if (cd.head != null) throw (new Exception("make_cd_head: the head already there: " + pn.value));
            string pn_value = (string)pn.value;
            if (pn_value.StartsWith("head>") == true)
            {
                make_cd_head(cd, (ParseNode)pn.children[0]);
                return;
            }
            string phrase = (string)concept.all_phrases[pn_value];
            if (phrase == null) phrase = pn_value;
            cd.head = (concept)concept.all_concepts[phrase];
            if (cd.head == null) throw (new Exception("make_cd_head: unrecognized concept: " + phrase));
        }

        // if prop_name is not an empty string "" then it over-rides the parse node value,
        // otherwise, the property name is obtained from the node value
        private void make_cd_property(CD cd, ParseNode pn, string prop_name)
        {
            string pn_value = pn.value;
            if (pn.children == null || pn.children.Count == 0) return; // ignore it
            if (pn_value.StartsWith("skip>") == true) // this means that the children of the node become properties of cd
            {
                foreach (ParseNode p in pn.children) { make_cd_property(cd, p, prop_name); };
                return;
            }
            if (pn_value.StartsWith("pvp>") == true) //this signals that the node has two children: property name and property value
            {
                make_pvp(cd, pn, "");
                return;
            }
            if (pn.children.Count != 1) throw (new Exception("make_cd_property: number of children != 1, pn.value = " + pn.value));
            ParseNode child = (ParseNode)pn.children[0];
            if (pn_value.StartsWith("head>") == true)
            {
                make_cd_head(cd, child);
                return;
            }
            if (pn_value.StartsWith("cd>") == true) // this signals that the property value is a cd, not a literal
            {
                if (prop_name == "") prop_name = pn_value.Substring(3);
                cd.PropList.Add(prop_name,new CD(child));
                return;
            }
            if (pn_value == "num:")
            {
                object child_value_obj = concept.all_nums[(string)child.value];
                if (child_value_obj == null) throw (new Exception("make_cd_property: unknown number, pn.value = " + pn.value));
                int child_value_num = (int)child_value_obj;
                if (prop_name == "") prop_name = "num:";
                cd.PropList.Add(prop_name, child_value_num);
                return;
            }
            string child_value = (string)concept.all_phrases[pn_value];
            if (child_value == null) child_value = pn_value;
            if (prop_name == "") prop_name = pn_value;
            cd.PropList.Add(prop_name, child.value);
        }

        private void make_pvp(CD cd, ParseNode pn, string prop_name)
        {
            if (prop_name == "")
            {
                foreach (ParseNode p in pn.children)
                {
                    string child_value = p.value;
                    if (child_value.StartsWith("propn>") == true)
                    {
                        ParseNode n = (ParseNode)p.children[0];
                        string str = n.value;
                        prop_name = (string)concept.all_phrases[str];
                        if (prop_name == null) prop_name = str;
                        break;
                    }
                }
            }
            if (prop_name == "") throw (new Exception("make_pvp: cannot find property name, pn.value = " + pn.value));
            foreach (ParseNode p in pn.children)
            {
                string child_value = p.value;
                if (child_value.StartsWith("cd>") == true)
                {
                    ParseNode grandchild = (ParseNode)p.children[0];
                    cd.PropList.Add(prop_name, new CD(grandchild));
                    break;
                }
            }
        }

        public string ToSexp()
        {
            string res = "(" + head.concept_name;
            string pname;
            object pval;
            string pval_string;
            if (PropList != null)
            {
                res = res + " (";
                foreach (DictionaryEntry d in PropList)
                {
                    pname = (string)d.Key;
                    pval = d.Value;
                    if (d.Value is CD)
                    {
                        CD x = (CD)d.Value;
                        pval_string = x.ToSexp();
                    }
                    else
                    {
                        pval_string = d.Value.ToString();
                    }
                    res = res + " (" + pname + " " + pval_string + ")";
                }
                res = res + ")";
            }
            return res + ")";
        }

    }

    public class concept
    {
        public static Hashtable all_concepts, all_phrases, all_nums;
        public string concept_name;
        public concept is_a;
        public Hashtable properties; // in case we want to further specify a concept

        public concept(string name)
        {
            concept_name = name;
            concept.all_concepts.Add(name, this);
        }


        public concept(string name, string parent)
        {
            concept_name = name;
            properties = new Hashtable();
            if (parent != "")
            {
                object parent_concept = concept.all_concepts[parent];
                if (parent_concept != null) is_a = (concept)parent_concept;
            }
            concept.all_concepts.Add(name, this);
        }

        public bool test_isa(concept parent)
        {
            if (this == parent) return true;
            if (this.is_a == null) return false;
            if (this.is_a == parent) return true;
            return this.is_a.test_isa(parent);
        }

        public static void initConcepts()
        {
            concept.all_concepts = new Hashtable();
            concept x;
            x = new concept("Anything");
            x = new concept("PhysObj", "Anything");
            x = new concept("Action", "Anything");
            x = new concept("AnimateObj", "PhysObj");
            x = new concept("InanimateObj", "PhysObj");
            x = new concept("avatar", "AnimateObj");
            x = new concept("master", "avatar");
            x = new concept("you", "avatar");
            x = new concept("me", "master");
            x = new concept("turn", "Action");
            x = new concept("turn_to_object", "Action");
            x = new concept("turn_around", "Action");
            x = new concept("go", "Action");
            x = new concept("go_to_object", "Action");
            x = new concept("go_to_center", "Action");
            x = new concept("pick_up_object", "Action");
            x = new concept("drop", "Action");
            x = new concept("report", "Action");
            x = new concept("it", "PhysObj");
            x = new concept("cube", "PhysObj");
            x.properties.Add("average_height", 1.0F);
            x = new concept("house", "PhysObj");
            x.properties.Add("average_height", 8.0F);
            x = new concept("tree", "PhysObj");
            x.properties.Add("average_height", 6.0F);
            x = new concept("pine", "tree");
            x.properties.Add("average_height", 6.0F);
            x = new concept("ball", "PhysObj");
            x.properties.Add("average_height", .6F);
            x = new concept("wall", "PhysObj");
            x.properties.Add("average_height", 3.0F);

            concept.all_phrases = new Hashtable();
            concept.all_phrases.Add("turn to", "turn_to_object");
            concept.all_phrases.Add("turn around", "turn_around");
            concept.all_phrases.Add("go to", "go_to_object");
            concept.all_phrases.Add("go to center", "go_to_center");
            concept.all_phrases.Add("pick up", "pick_up_object");

            concept.all_phrases.Add("to the left", "left");
            concept.all_phrases.Add("on the left", "left");
            concept.all_phrases.Add("on your left", "left");
            concept.all_phrases.Add("to the left of", "left");

            concept.all_phrases.Add("to the right", "right");
            concept.all_phrases.Add("on the right", "right");
            concept.all_phrases.Add("on your right", "right");
            concept.all_phrases.Add("to the right of", "right");

            concept.all_phrases.Add("to the east", "east");
            concept.all_phrases.Add("to the east of", "east");
            concept.all_phrases.Add("east of", "east");

            concept.all_phrases.Add("to the west", "west");
            concept.all_phrases.Add("to the west of", "west");
            concept.all_phrases.Add("west of", "west");

            concept.all_phrases.Add("to the north", "north");
            concept.all_phrases.Add("to the north of", "north");
            concept.all_phrases.Add("north of", "north");

            concept.all_phrases.Add("to the south", "south");
            concept.all_phrases.Add("to the south of", "south");
            concept.all_phrases.Add("south of", "south");

            concept.all_phrases.Add("nearby", "near");
            concept.all_phrases.Add("in front of", "front");

            concept.all_phrases.Add("box", "cube");

            concept.all_nums = new Hashtable();
            concept.all_nums.Add("one", 1);
            concept.all_nums.Add("two", 2);
            concept.all_nums.Add("three", 3);
            concept.all_nums.Add("four", 4);
            concept.all_nums.Add("five", 5);
            concept.all_nums.Add("six", 6);
            concept.all_nums.Add("seven", 7);
            concept.all_nums.Add("eight", 8);
            concept.all_nums.Add("nine", 9);
            concept.all_nums.Add("ten", 10);
            concept.all_nums.Add("twenty", 20);
            concept.all_nums.Add("thirty", 30);
            concept.all_nums.Add("forty five", 45);
            concept.all_nums.Add("sixty", 60);
            concept.all_nums.Add("ninety", 90);

        }
    }
}
