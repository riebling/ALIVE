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
using SimpleGrammar;

namespace DogsBrain
{
    public class DogsMind
    {
        public ALIVE.SmartDog myDog;
        public Hashtable knownObjects;
        public AliveObject[,] obstacleMap; // object occupying this grid point
        public bool[,] exploredMap; // true if explored
        public int min_x, min_y, max_x, max_y; //limits to dog's meandering
        public Context myContext;
        public sgrMachine ParseMachine;
        public SpeechSynthesizer mySynth;
        public SpeechRecognitionEngine myRecog;
        public MyBot.BotControlForm1.objectsTextBoxUpdater oboxSay;

        public DogsMind(ALIVE.SmartDog dog, SpeechSynthesizer myS, SpeechRecognitionEngine myR, sgrMachine pm)
        {
            myDog = dog;
            mySynth = myS;
            myRecog = myR;
            min_x = 5;
            min_y = 5;
            max_x = 251;
            max_y = 251;
            obstacleMap = new AliveObject[256, 256];
            exploredMap = new bool[256, 256];
            myContext = new Context();
            knownObjects = new Hashtable();
            update_explored();
            dogGrammars.buildDogGrammars(this);
            ParseMachine = pm;
            myDog.PlayAnimation(AliveAnimation.HELLO);
            mySynth.SpeakAsync("I am ready master!");
        }


        public List<AliveObject> update_explored()
        {
            List<AliveObject> newObjects;
            update_exploredMap();
            List<AliveObject> foundObjects = myDog.ObjectsAround();
            if (foundObjects != null)
            {
                newObjects = update_knownObjects(foundObjects);
                return newObjects;
            }
            this.myContext.new_objects = null;
            return null;
        }

        private void update_exploredMap()
        {
            float x, y;
            myDog.Coordinates(out x, out y);
            int xx = (int)x;
            int yy = (int)y;
            int wb = xx - 10;
            int sb = yy - 10;
            for (int i = 1; i < 21; i++)
            {
                for (int j = 1; j < 21; j++)
                {
                    int xi = wb + i;
                    int yj = sb + j;
                    if (xi >= 0 && xi <= 255 && yj >= 0 && yj <= 255 &&
                        (xi - x) * (xi - x) + (yj - y) * (yj - y) <= 100) exploredMap[xi, yj] = true;
                }
            }
        }

        private List<AliveObject> update_knownObjects(List<AliveObject> foundObjects)
        {
            int xx, yy;
            string fam, names;
            uint lid;
            Hashtable knownFamObjects;
            AliveObject oldOb;
            List<AliveObject> res = new List<AliveObject>();
            foreach (AliveObject fob in foundObjects)
            {
                fam = fob.family.ToLower();
                if (fam == "" || concept.all_concepts[fam] == null)
                {
                    Console.WriteLine("The family of " + fob.name + " = " + fam + " is not a known concept");
                    break;
                }
                lid = fob.LocalID;
                xx = (int)Math.Round(fob.X);
                yy = (int)Math.Round(fob.Y);
                knownFamObjects = (Hashtable)knownObjects[fam];
                if (knownFamObjects == null)
                {
                    knownFamObjects = new Hashtable();
                    knownFamObjects.Add(lid, fob);
                    knownObjects.Add(fam, knownFamObjects);
                    update_obstacleMap(fob);
                    res.Add(fob);
                }
                else
                {
                    oldOb = (AliveObject)knownFamObjects[lid];
                    if (oldOb != null)
                    {
                        if (oldOb.X != fob.X || oldOb.Y != fob.Y)
                        {
                            oldOb.X = fob.X;
                            oldOb.Y = fob.Y;
                            res.Add(oldOb);
                            removeObstacle(oldOb);
                            update_obstacleMap(fob);
                        }
                    }
                    else
                    {
                        knownFamObjects.Add(lid, fob);
                        update_obstacleMap(fob);
                        res.Add(fob);
                    }
                }
            }
            this.myContext.new_objects = res;
            names = "";
            foreach (AliveObject x in res) { names = names + " " + x.name; };
            Console.WriteLine("I sense " + res.Count.ToString() + " new or moved objects: " + names);
            return res;
        }

        private void removeObstacle(AliveObject fob)
        {
            for (int i = 0; i < 256; i++)
            {
                for (int j = 0; j < 256; j++)
                {
                    if (obstacleMap[i, j] == fob) obstacleMap[i, j] = null;
                }
            }
        }

        private void update_obstacleMap(AliveObject fob)
        {
            double w, d, r, xc, yc, x1, y1, x2, y2, angle, delta;
            int xleft, ybot, xright, ytop;
            w = fob.width / 2;
            d = fob.depth / 2;
            r = Math.Sqrt(w * w + d * d);
            if (r < .35) return; // small objects are not obstacles
            xc = fob.X;
            yc = fob.Y;
            angle = Math.PI * fob.angle / 180;
            delta = 1.1;
            xleft = (int)Math.Max(Math.Floor(xc - r), 0);
            ybot = (int)Math.Max(Math.Floor(yc - r), 0);
            xright = (int)Math.Min(Math.Ceiling(xc + r), 255);
            ytop = (int)Math.Min(Math.Ceiling(yc + r), 255);
            for (int x = xleft; x <= xright; x++)
            {
                for (int y = ybot; y <= ytop; y++)
                {
                    x1 = x - xc;
                    y1 = y - yc;
                    x2 = x1 * Math.Cos(angle) + y1 * Math.Sin(angle);
                    y2 = -x1 * Math.Sin(angle) + y1 * Math.Cos(angle);
                    if (Math.Abs(x2) < w + delta && Math.Abs(y2) < d + delta)
                    {
                        obstacleMap[x, y] = fob;
                        exploredMap[x, y] = true;
                    }
                }
            }
        }

        internal void updateObjLoc(AliveObject tob)
        {
            float x, y;
            myDog.Coordinates(out x, out y);
            tob.X = x;
            tob.Y = y;
        }

        // Finds the nearest free grid point
        public bool freeGridPoint(float x, float y, out int xi, out int yi)
        {
            int cx, cy;
            xi = (int)Math.Round(x);
            yi = (int)Math.Round(y);
            if (xi < min_x || xi > max_x || yi < min_y || yi > max_y) return false; // out of range
            if (obstacleMap[xi, yi] == null) return true;
            cx = (int)x;
            cy = (int)y;
            for (int i = 0; i < 15; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    xi = cx - j; // lower left quadrant
                    yi = cy - (i - j);
                    if (xi >= min_x && yi >= min_y && obstacleMap[xi, yi] == null) return true;
                    yi = cy + 1 + (i - j); //upper left quadrant
                    if (xi >= min_x && yi <= max_y && obstacleMap[xi, yi] == null) return true;
                    xi = cx + 1 + j; // lower right quadrant
                    yi = cy - (i - j);
                    if (xi <= max_x && yi >= min_y && obstacleMap[xi, yi] == null) return true;
                    yi = cy + 1 + (i - j); // upper right quadrant
                    if (xi <= max_x && yi <= max_y && obstacleMap[xi, yi] == null) return true;
                }
            }
            return false;
        }

        public float distance(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

    }
   
}