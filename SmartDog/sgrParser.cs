using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SimpleGrammar
{

    //This builds a simple grammar finite-state machine
    //that consists of state nodes and transitions
    //Transitions correspond to the terminal nodes in the simple grammar
    public class StateNode
    {
        public Hashtable transitions;
        public bool end_marker;
        public string name;
        public static int count = 0;
        public static Hashtable allStates;

        public StateNode()
        {
            end_marker = false;
            transitions = new Hashtable();
            name = "State" + count.ToString();
            count = count + 1;
            allStates.Add(name, this);
        }
    }

    public class Transition
    {
        public StateNode start;
        public StateNode end;
        public string value;
        public bool terminal;
        public Transition parent;
        public sgrNode gr_node;
        public ParseNode parse_node;
        public static ArrayList allTransitions;

        public Transition(string val, StateNode s, StateNode e, sgrNode sn)
        {
            start = s;
            end = e;
            value = val;
            gr_node = sn;
            allTransitions.Add(this);
        }
    }

    public class sgrMachine
    {
        public StateNode start_state;
        public StateNode end_state;
        public Transition top_trans;

        public sgrMachine(sgrNode root)
        {
            StateNode.count = 0;
            StateNode.allStates = new Hashtable();
            Transition.allTransitions = new ArrayList();
            start_state = new StateNode();
            end_state = new StateNode();
            end_state.end_marker = true;
            top_trans = new Transition(root.name, start_state, end_state, root);
            if (root.node_type == "T")
            {
                top_trans.terminal = true;
                addTrans(top_trans);
            }
            else
            {
                top_trans.terminal = false;
                expandTrans(top_trans);
            }
        }

        //Non-terminal transitions are recursively expanded
        //(constructing new state nodes,if necessary)
        //until only terminal transitions are left
        //(corresponding to the terminal nodes in the grammar)
        private void expandTrans(Transition trans)
        {
            string nt = trans.gr_node.node_type;
            if (nt == "+") expandSeq(trans);
            else
            {
                if (nt == "|") expandOr(trans);
                else throw new Exception("expandTrans cannot expand transition");
            }
        }

        private void expandSeq(Transition trans)
        {
            StateNode cur_start;
            StateNode cur_end;
            Transition cur_trans;
            sgrNode child;
            ArrayList chldrn = trans.gr_node.children;
            if (chldrn == null) throw new Exception("expandSeq argument has no children");
            int n = chldrn.Count;
            cur_start = trans.start;
            for (int i = 0; i < n; i++)
            {
                if (i == n - 1) cur_end = trans.end; else cur_end = new StateNode();
                child = (sgrNode)chldrn[i];
                cur_trans = new Transition(child.name, cur_start, cur_end, child);
                cur_trans.parent = trans;
                if (child.optional == true)
                {
                    Transition lambda = new Transition("lambda", cur_start, cur_end, child);
                    lambda.terminal = true;
                    lambda.parent = cur_trans;
                    addTrans(lambda);
                }
                if (child.node_type == "T")
                {
                    cur_trans.terminal = true;
                    addTrans(cur_trans);
                }
                else
                {
                    cur_trans.terminal = false;
                    expandTrans(cur_trans);
                }
                cur_start = cur_end;
            }
        }

        private void expandOr(Transition trans)
        {
            Transition cur_trans;
            ArrayList chldrn = trans.gr_node.children;
            if (chldrn == null || chldrn.Count == 0) throw new Exception("expandOr argument has no children");
            foreach (sgrNode child in chldrn)
            {
                cur_trans = new Transition(child.name, trans.start, trans.end, child);
                cur_trans.parent = trans;
                if (child.optional == true) throw new Exception("Optional child is expandOr");
                if (child.node_type == "T")
                {
                    cur_trans.terminal = true;
                    addTrans(cur_trans);
                }
                else
                {
                    cur_trans.terminal = false;
                    expandTrans(cur_trans);
                }
            }
        }

        private void addTrans(Transition trans)
        {
            if (trans.terminal == false) return;
            StateNode start = trans.start;
            Hashtable trs = start.transitions;
            if (trs == null) trs = new Hashtable();
            ArrayList bucket = (ArrayList)trs[trans.value];
            if (bucket == null)
            {
                bucket = new ArrayList();
                trs.Add(trans.value, bucket);
            }
            bucket.Add(trans);
        }

        public void initMachine()
        {
            foreach (Transition tr in Transition.allTransitions) tr.parse_node = null;
        }

    }

    //This does actual parsing of a sentence using an sgrMachine
    //The working structure is an array of MatchPoints - one for each word in the input sentence
    //Each MatchPoint maintains the list of all possible states matching the input so far
    //This list is maintained as a hashtable indexed by the state names with buckets of transitions
    //leading to that state
    public class ParseTree
    {
        public MatchPoint[] match_points;
        public string[] words;
        public ParseNode root;
        public sgrMachine machine;

        public ParseTree(sgrMachine m, string sentence)
        {
            Console.WriteLine("Parsing: " + sentence);
            machine = m;
            machine.initMachine();
            words = sentence.Split();
            int wc = words.Length;
            match_points = new MatchPoint[wc + 1];
            for (int i = 0; i <= wc; i++) match_points[i] = new MatchPoint();
            match_points[0].states.Add(m.start_state.name, new ArrayList());//Start dummy
            for (int i = 0; i < wc; i++) doMatchPoint(i);
            doLambdas(wc);
            if (match_points[wc].states.Count == 0) return;
            ArrayList leaves = parseLeaves();
            if (leaves == null || leaves.Count == 0) return;
            root = buildTree(leaves); // This builds the final output tree of parse nodes
        }

        private void doMatchPoint(int w_num)
        {
            doLambdas(w_num);
            MatchPoint mp = match_points[w_num];
            foreach (DictionaryEntry d in mp.states)
            {
                StateNode sn = (StateNode)StateNode.allStates[d.Key];
                if (sn != null && sn.end_marker == false)
                {
                    ArrayList m_trs = (ArrayList)sn.transitions[words[w_num]];
                    if (m_trs != null && m_trs.Count > 0)
                    {
                        foreach (Transition tr in m_trs)
                        {
                            match_points[w_num + 1].addTrans(tr);
                        }
                    }
                }
            }
        }

        private void doLambdas(int w_num)
        {
            MatchPoint mp = match_points[w_num];
            ArrayList lambdas = new ArrayList();
            ArrayList new_lambdas = new ArrayList();
            foreach (DictionaryEntry d in mp.states)
            {
                StateNode sn = (StateNode)StateNode.allStates[d.Key];
                if (sn != null && sn.end_marker == false)
                {
                    ArrayList l_trs = (ArrayList)sn.transitions["lambda"];
                    if (l_trs != null) lambdas.AddRange(l_trs);
                }
            }
            foreach (Transition tr in lambdas)
            {
                ArrayList trs = collectLambdas(tr);
                if (trs != null) new_lambdas.AddRange(trs);
            }
            foreach (Transition tr in new_lambdas) mp.addTrans(tr);
        }

        private ArrayList collectLambdas(Transition trans)
        {
            ArrayList res = new ArrayList();
            if (trans.value != "lambda") return res;
            res.Add(trans);
            StateNode sn = trans.end;
            ArrayList trs = (ArrayList)sn.transitions["lambda"];
            if (trs != null)
            {
                foreach (Transition tr in trs)
                {
                    ArrayList ntrs = collectLambdas(tr);
                    if (ntrs != null) res.AddRange(ntrs);
                }
            }
            return res;
        }

        private ArrayList parseLeaves()
        {
            ArrayList res = new ArrayList();
            int wc = words.Length;
            StateNode cur_state = machine.end_state;
            for (int i = wc; i > 0; i--)
            {
                ArrayList trs = (ArrayList)match_points[i].states[cur_state.name];
                if (trs == null || trs.Count == 0) return null;
                Transition tr = skipLambdas((Transition)trs[0], i);
                res.Insert(0, tr);
                cur_state = tr.start;
            }
            return res;
        }

        private Transition skipLambdas(Transition tr, int i)
        {
            if (tr.value != "lambda") return tr;
            ArrayList trs = (ArrayList)match_points[i].states[tr.start.name];
            return skipLambdas((Transition)trs[0], i);
        }

        private ParseNode buildTree(ArrayList leaves)
        {
            ParseNode.root = null;
            foreach (Transition tr in leaves)
            {
                ParseNode pn = new ParseNode(tr);
                pn.terminal = true;
            }
            return ParseNode.root;
        }
    }

    public class ParseNode
    {
        public ArrayList children;
        public ParseNode parent;
        public Transition trans;
        public string value;
        public bool terminal = false;
        public static ParseNode root;

        public ParseNode(string x)
        {
            value = x;
            terminal = true;
        }

        public ParseNode(Transition tr)
        {
            value = tr.value;
            children = new ArrayList();
            trans = tr;
            tr.parse_node = this;
            Transition parent_tr = tr.parent;
            if (parent_tr == null)
            {
                root = this;
                return;
            }
            ParseNode pn = parent_tr.parse_node;
            if (pn != null)
            {
                pn.children.Add(this);
                parent = pn;
                return;
            }
            pn = new ParseNode(parent_tr);
            pn.children.Add(this);
            parent = pn;
        }

        public void merge_W_leaves()
        {
            if (children == null || children.Count == 0) return;
            if (value == "W")
            {
                string new_name = "";
                foreach (ParseNode child in children) new_name = new_name + child.value + " ";
                value = new_name.TrimEnd();
                children = null;
                terminal = true;
            }
            else
            {
                foreach (ParseNode child in children) child.merge_W_leaves();
            }
        }

        public string toSexp()
        {
            if (children == null || children.Count == 0) return value;
            string res = "(" + value + " ";
            foreach (ParseNode child in children) res = res + " " + child.toSexp();
            return res + ")";
        }
    }

    public class MatchPoint
    {
        public Hashtable states;

        public MatchPoint()
        {
            states = new Hashtable();
        }

        public void addTrans(Transition trans)
        {
            StateNode st = trans.end;
            ArrayList trans_bucket = (ArrayList)states[st.name];
            if (trans_bucket == null)
            {
                trans_bucket = new ArrayList();
                states.Add(st.name, trans_bucket);
            }
            if (trans_bucket.Contains(trans) == false) trans_bucket.Add(trans);
        }
    }


}
