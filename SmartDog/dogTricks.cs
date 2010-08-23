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
    public static class dogTricks
    {
        
        //walks to an object, tries 25 times
        public static bool walk_to_point(DogsMind myMind, AliveObject target)
        {
            Console.WriteLine("walkToPoint: Walking to <" + target.name + "> at <" + target.X.ToString() + "," + target.Y.ToString() + ">");
            for (int i = 0; i < 25; i++)
            {
                if (walkTo(myMind, target) == true) return true;
                myMind.update_explored(); //If not at target, look around and try again
            }
            return false;
        }


        //Tries reaching a grid point up to 25 times
        public static bool walk_to_point(DogsMind myMind, float x, float y)
        {
            Console.WriteLine("walkToPoint: Walking to <" + x.ToString() + "," + y.ToString() + ">");
            for (int i = 0; i < 25; i++)
            {
                if (walkTo(myMind, x, y) == true) return true;
                myMind.update_explored();
            }
            return false;
        }

        //This method creates a Dijkstra path to the target and tries to follow it until it hits an unexplored grid point
        public static bool walkTo(DogsMind myMind, AliveObject target)
        {
            float xc, yc, dist;
            myMind.myDog.Coordinates(out xc, out yc);
            dist = distance(xc, yc, target.X, target.Y);
            Console.WriteLine("walkTo: Walking from <" + xc.ToString() + "," + yc.ToString() + "> to <" + target.name + "> at <" + target.X.ToString() + "," + target.Y.ToString() + ">");
            if (dist < 1) return true;
            walkPath wpath = new walkPath(xc, yc, target, myMind); //this creates the dijkstra path
            Console.WriteLine("walkTo: Path = " + wpath.ToMessage(wpath.path));
            if (wpath.path.Count == 0) return false;
            return wpath.followPath(); //this follows the path either to the end or to the first unexplored grid point
        }

        //Same as above, but walks to a point, instead of a target object
        public static bool walkTo(DogsMind myMind, float xt, float yt)
        {
            float xc, yc, dist;
            myMind.myDog.Coordinates(out xc, out yc);
            dist = distance(xc, yc, xt, yt);
            Console.WriteLine("walkTo: Walking from <" + xc.ToString() + "," + yc.ToString() + "> to <" + xt.ToString() + "," + yt.ToString() + ">");
            if (dist < 1) return true;
            walkPath wpath = new walkPath(xc, yc, xt, yt, myMind);
            Console.WriteLine("walkTo: Path = " + wpath.ToMessage(wpath.path));
            if (wpath.path.Count == 0) return false;
            return wpath.followPath();
        }

        //walk to a point but stop after 10 steps
        public static bool walkTo(DogsMind myMind, float xt, float yt, int lim)
        {
            float xc, yc, dist;
            myMind.myDog.Coordinates(out xc, out yc);
            dist = distance(xc, yc, xt, yt);
            Console.WriteLine("walkTo: Walking from <" + xc.ToString() + "," + yc.ToString() + "> to <" + xt.ToString() + "," + yt.ToString() + ">, limit " + lim + " steps");
            if (dist < 1) return true;
            walkPath wpath = new walkPath(xc, yc, xt, yt, myMind);
            Console.WriteLine("walkTo: Path = " + wpath.ToMessage(wpath.path));
            if (wpath.path.Count == 0) return false;
            return wpath.followPath(lim);
        }


        public static float distance(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

//======================================================================================
        //Matches an object to a conceptual description
        public static float matchObj(AliveObject obj, CD descr, DogsMind myMind)
        {
            Console.WriteLine("Matching " + obj.name + " to " + descr.ToSexp());
            string fam = obj.family.ToLower();
            concept obj_concept = (concept)concept.all_concepts[fam];
            if (obj_concept == null) return 0;
            concept descr_concept = descr.head;
            if (descr_concept == null) return 0;
            if (obj_concept.test_isa(descr_concept) == false) return 0;
            float res = 1.0F;
            foreach (DictionaryEntry i in descr.PropList) // check color and size first
            {
                string prop = (string)i.Key;
                switch (prop)
                {
                    case "color:":
                        res = res * matchColor(obj, (string)i.Value, myMind);
                        break;
                    case "size:":
                        res = res * matchSize(obj, (string)i.Value, myMind);
                        break;
                    default:
                        break;
                }
                if (res < .1) break;
            }
            if (res >= .1)
            {
                foreach (DictionaryEntry i in descr.PropList)
                {
                    string prop = (string)i.Key;
                    switch (prop)
                    {
                        case "near":
                            res = res * matchNear(obj, (CD)i.Value, myMind);
                            break;
                        case "behind":
                        case "front":
                        case "left":
                        case "right":
                        case "north":
                        case "south":
                        case "east":
                        case "west":
                            res = res * matchDirOf(obj, (CD)i.Value, myMind, prop);
                            break;
                        default:
                            break;
                    }
                    if (res < .1) break;
                }
            }
            Console.WriteLine("Result of matching " + obj.name + " to " + descr.ToSexp() + " is " + res.ToString());
            return res;
        }

        //Colors are matched literally
        private static float matchColor(AliveObject obj, string d, DogsMind myMind)
        {
            float res = 1F;
            if (d != obj.color) res = 0;
            Console.WriteLine("Matching color of " + obj.name + " to " + d + " Result: " + res.ToString());
            return res;
        }

        //Sizes are matched based on the average height of the object family
        //We may want to start with a default average height, but then re-calculate it after we've seen a few objects
        private static float matchSize(AliveObject obj, string d, DogsMind myMind)
        {
            float res = 0;
            float obj_height = obj.height;
            string fam = obj.family.ToLower();
            concept obj_concept = (concept)concept.all_concepts[fam];
            object h = obj_concept.properties["average_height"];
            float avg_height = (float)h;
            switch (d)
            {
                case "small":
                    if (obj_height <= .5 * avg_height) { res = 1.0F; break; }
                    if (obj_height > 1.5 * avg_height) { res = 0; break; }
                    res = 1.5F - obj_height / avg_height;
                    break;
                case "big":
                case "tall":
                    if (obj_height >= 1.5 * avg_height) { res = 1.0F; break; }
                    if (obj_height < .5 * avg_height) { res = 0; break; }
                    res = obj_height / avg_height - .5F;
                    break;
                default:
                    break;
            }
            Console.WriteLine("Matching size of " + obj.name + " to " + d + " Result " + res.ToString());
            return res;
        }

        //We stretch "nearness" with the square root of the height of the object
        //5 meters is "nearer" to a tall tree than to a short tree - this may be an overkill
        private static float matchNear(AliveObject obj, CD descr, DogsMind myMind)
        {
            Console.WriteLine("Matching near");
            concept descr_concept = descr.head;
            if (descr_concept == null) return 0;
            if (descr_concept.concept_name == "you") return matchNearYou(obj, myMind);
            float best = 0;
            foreach (DictionaryEntry i in myMind.knownObjects)
            {
                string fam = (string)i.Key;
                concept obj_concept = (concept)concept.all_concepts[fam];
                if (obj_concept.test_isa(descr_concept) == true)
                {
                    Hashtable knownFamObjects = (Hashtable)i.Value;
                    foreach (DictionaryEntry j in knownFamObjects)
                    {
                        AliveObject obj2 = (AliveObject) j.Value;
                        float zz = matchObj(obj2, descr, myMind);
                        float h = Math.Max(obj2.height, 1);
                        float x_diff = obj2.X - obj.X;
                        float y_diff = obj2.Y - obj.Y;
                        float dist = (float)Math.Sqrt(((x_diff * x_diff) + (y_diff * y_diff))/h);
                        float ww;
                        if (dist < 3F)
                        {
                            ww = 1.0F;
                        }
                        else
                        {
                            if (dist > 7F)
                            {
                                ww = 0;
                            }
                            else
                            {
                                ww = (7F - dist) / 4F;
                            }
                        }
                        zz = zz * ww;
                        Console.WriteLine("Nearness degree between " + obj.name + " and " + obj2.name + " = " + ww.ToString());
                        if (zz > best) best = zz;
                    }
                }
            }
            Console.WriteLine("Best confidence for near: " + best.ToString());
            return best;
        }

        private static float matchNearYou(AliveObject obj, DogsMind myMind)
        {
            float x_diff = myMind.myContext.start_pos_x - obj.X;
            float y_diff = myMind.myContext.start_pos_y - obj.Y;
            float dist = (float)Math.Sqrt((x_diff * x_diff) + (y_diff * y_diff));
            float ww;
            if (dist < 3F)
            {
                ww = 1.0F;
            }
            else
            {
                if (dist > 7F)
                {
                    ww = 0;
                }
                else
                {
                    ww = (7F - dist) / 4F;
                }
            }
            Console.WriteLine("Near degree = " + ww.ToString());
            return ww;
        }


        private static float matchDirOf(AliveObject obj, CD descr, DogsMind myMind, string dir)
        {
            float dog_x, dog_y, delta_x, delta_y;
            Console.WriteLine("Matching " + dir);
            concept descr_concept = descr.head;
            if (descr_concept == null) { Console.WriteLine("Reference concept = null, result of matching " + dir + " = 0"); return 0; }
            if (descr_concept.concept_name == "you") return matchDirYou(obj, myMind, dir);
            float best = 0;
            dog_x = myMind.myContext.start_pos_x;
            dog_y = myMind.myContext.start_pos_y;
            foreach (DictionaryEntry i in myMind.knownObjects) // go through all matching reference objects and pick the best
            {
                string fam = (string)i.Key;
                concept obj_concept = (concept)concept.all_concepts[fam];
                if (obj_concept.test_isa(descr_concept) == true) //Got the right family, e.g., tree or ball
                {
                    Hashtable knownFamObjects = (Hashtable)i.Value;
                    foreach (DictionaryEntry j in knownFamObjects)
                    {
                        AliveObject obj2 = (AliveObject)j.Value;
                        float zz = matchObj(obj2, descr, myMind); //match the reference object to its description
                        //delta_x is the x'*dist(dog's starting position,obj2), where x' is the x coordinate (east) when the origin is at the center of obj2
                        //and north is aligned with the direction from the dog's starting position and obj2
                        //Only the sign of deltas is important and we use it only for front, behind, left and right
                        delta_x = (obj.X - obj2.X) * (obj2.Y - dog_y) - (obj.Y - obj2.Y) * (obj2.X = dog_x);
                        delta_y = (obj.X - obj2.X) * (obj2.X - dog_x) + (obj.Y - obj2.Y) * (obj2.Y - dog_y);
                        switch (dir)
                        {
                            case "behind":
                                if (delta_y > 0) zz = 0;
                                break;
                            case "front":
                                if (delta_y < 0) zz = 0;
                                break;
                            case "left":
                                if (delta_x > 0) zz = 0;
                                break;
                            case "right":
                                if (delta_x < 0) zz = 0;
                                break;
                            case "north":
                                if (obj.Y < obj2.Y) zz = 0;
                                break;
                            case "south":
                                if (obj.Y > obj2.Y) zz = 0;
                                break;
                            case "east":
                                if (obj.X < obj2.X) zz = 0;
                                break;
                            case "west":
                                if (obj.X > obj2.X) zz = 0;
                                break;
                            default:
                                break;
                        }
                        Console.WriteLine("After matching dir " + dir + " degree of match is " + zz.ToString());
                        if (zz > best) best = zz;
                        }
                    }
                }
            Console.WriteLine("Best conf for dir " + dir + " = " + best.ToString());
            return best;
        }

        private static float matchDirYou(AliveObject obj, DogsMind myMind, string dir)
        {
            float obj_x, obj_y, res;
            Console.WriteLine("Matching " + dir + " relative to the dog's position at the beginning of the task");
            res = 0;
            change_origin(myMind.myContext.start_pos_x, myMind.myContext.start_pos_y, myMind.myContext.start_angle, obj.X, obj.Y, out obj_x, out obj_y);
            switch (dir)
            {
                case "behind":
                    if (obj_y < 0) res = 1;
                    break;
                case "front":
                    if (obj_y > 0) res = 1;
                    break;
                case "left":
                    if (obj_x < 0) res = 1;
                    break;
                case "right":
                    if (obj_x > 0) res = 1;
                    break;
                case "north":
                    if (obj.Y > myMind.myContext.start_pos_y) res = 1;
                    break;
                case "south":
                    if (obj.Y < myMind.myContext.start_pos_y) res = 1;
                    break;
                case "east":
                    if (obj.X > myMind.myContext.start_pos_x) res = 1;
                    break;
                case "west":
                    if (obj.X < myMind.myContext.start_pos_x) res = 1;
                    break;
                default:
                    break;
            }
            Console.WriteLine("Result of Matching " + dir + " relative to the dog's position at the beginning of the task = " + res.ToString());
            return res;
        }

//===========================================================================================
        // Selecting objects that match the description

        public static float selectKnowObject(DogsMind dm, CD descr, out AliveObject res)
        {
            res = null;
            concept descr_concept = descr.head;
            if (descr_concept == null) return 0;
            Console.WriteLine("Select Known Object matching: " + descr.ToSexp());
            Console.WriteLine(dogPos(dm));
            float best = 0;
            float best_dist = 500F;
            foreach (DictionaryEntry i in dm.knownObjects)
            {
                string fam = (string)i.Key;
                concept obj_concept = (concept)concept.all_concepts[fam];
                if (obj_concept.test_isa(descr_concept) || descr_concept.test_isa(obj_concept))
                {
                    Hashtable knownFamObjects = (Hashtable)i.Value;
                    foreach (DictionaryEntry j in knownFamObjects)
                    {
                        AliveObject obj = (AliveObject) j.Value;
                        float zz = matchObj(obj, descr, dm);
                        if (zz > best)
                        {
                            best = zz;
                            res = obj;
                        }
                        else
                        {
                            if (zz == best) //all being equal, pick the nearest
                            {
                                float dogx;
                                float dogy;
                                dm.myDog.Coordinates(out dogx, out dogy);
                                float ds = distance(obj.X, obj.Y, dogx, dogy);
                                if (ds < best_dist)
                                {
                                    best_dist = ds;
                                    res = obj;
                                }
                            }
                        }
                    }
                }
            }
            if (res == null) Console.WriteLine("No known object matches the description");
            else Console.WriteLine("Best match is " + res.name + " degree = " + best.ToString());
            return best;
        }

        public static string dogPos(DogsMind dm)
        {
            float dx, dy, rot;
            dm.myDog.Coordinates(out dx, out dy);
            rot = dm.myDog.Orientation();
            string res = "Dog's position = <" + dx.ToString() + "," + dy.ToString() + "> Rotation = " + rot.ToString();
            return res;
        }

        //finds an unexplored grid point closest to <rel_x, rel_y> but within lim city blocks
        public static bool find_unexplored_rel(DogsMind myMind, float rel_x, float rel_y, out int xi, out int yi, int lim)
        {
            int cx, cy;
            int min_x = myMind.min_x;
            int min_y = myMind.min_y;
            int max_x = myMind.max_x;
            int max_y = myMind.max_y;
            xi = (int)Math.Round(rel_x);
            yi = (int)Math.Round(rel_y);
            if (xi < min_x || xi > max_x || yi < min_y || yi > max_y) return false;
            if (myMind.exploredMap[xi, yi] == false) return true;
            cx = xi;
            cy = yi;
            for (int i = 0; i < lim; i++)
            {
                for (int j = 0; j <= i; j++)
                {
                    xi = cx - j; // lower left quadrant
                    yi = cy - (i - j);
                    if (xi >= min_x && yi >= min_y && myMind.exploredMap[xi, yi] == false) return true;
                    yi = cy + 1 + (i - j); //upper left quadrant
                    if (xi >= min_x && yi <= max_y && myMind.exploredMap[xi, yi] == false) return true;
                    xi = cx + 1 + j; // lower right quadrant
                    yi = cy - (i - j);
                    if (xi <= max_x && yi >= min_y && myMind.exploredMap[xi, yi] == false) return true;
                    yi = cy + 1 + (i - j); // upper right quadrant
                    if (xi <= max_x && yi <= max_y && myMind.exploredMap[xi, yi] == false) return true;
                }
            }
            return false;
        }

        public static void change_origin(float c_x, float c_y, float angle, float old_x, float old_y, out float new_x, out float new_y)
        {
            double rad_angle = (double)angle * Math.PI / 180;
            new_x = (old_x - c_x) * (float)Math.Cos(rad_angle) - (old_y - c_y) * (float)Math.Sin(rad_angle);
            new_y = (old_x - c_x) * (float)Math.Sin(rad_angle) + (old_y - c_y) * (float)Math.Cos(rad_angle);
        }
    }
}
