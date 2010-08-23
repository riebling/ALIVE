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

    public class walkPath
    {
        public ArrayList path;
        public float[,] distmap;
        public int[,] backmapX, backmapY;
        public int start_x, start_y, end_x, end_y, min_x, min_y, max_x, max_y;
        public ArrayList open;
        public DogsMind myMind;

        public walkPath(float xc, float yc, AliveObject target, DogsMind mind)
        {
            min_x = mind.min_x;
            min_y = mind.min_y;
            max_x = mind.max_x;
            max_y = mind.max_y;
            myMind = mind;
            int[] node;
            path = new ArrayList();
            open = new ArrayList();
            backmapX = new int[256, 256];
            backmapY = new int[256, 256];
            myMind.freeGridPoint(xc, yc, out start_x, out start_y);
            end_x = (int)target.X;
            end_y = (int)target.Y;
            if (start_x < min_x || start_y < min_y || end_x < min_x || end_y < min_y ||
                start_x > max_x || start_y > max_y || end_x > max_x || end_y > max_y) return;
            if (start_x == end_x && start_y == end_y) return;
            backmapX[start_x, start_y] = start_x;
            backmapY[start_x, start_y] = start_y;
            node = new int[2] { start_x, start_y };
            open.Add(node);
            if (buildBackMaps(target) == false) return; // can't get there
            buildPath();
        }

        private bool buildBackMaps(AliveObject target)
        {
            if (open.Count == 0) return false;
            int[] c_node = (int[])open[0];
            int cx = c_node[0];
            int cy = c_node[1];
            if (next_to(cx, cy, target) || dogTricks.distance(cx,cy,target.X,target.Y) < 1.5)
            {
                end_x = cx;
                end_y = cy;
                return true;
            }
            open.RemoveAt(0);
            if (cx + 1 <= max_x && myMind.obstacleMap[cx + 1, cy] == null)
            {
                updOpen(cx, cy, 1, 0);
                if (cy + 1 <= max_y && myMind.obstacleMap[cx + 1, cy + 1] == null) updOpen(cx, cy, 1, 1);
                if (cy - 1 >= min_y && myMind.obstacleMap[cx + 1, cy - 1] == null) updOpen(cx, cy, 1, -1);
            }
            if (cx - 1 >= min_x && myMind.obstacleMap[cx + 1, cy] == null)
            {
                updOpen(cx, cy, -1, 0);
                if (cy + 1 <= max_y && myMind.obstacleMap[cx - 1, cy + 1] == null) updOpen(cx, cy, -1, 1);
                if (cy - 1 >= min_y && myMind.obstacleMap[cx - 1, cy - 1] == null) updOpen(cx, cy, -1, -1);
            }
            if (cy + 1 <= max_x && myMind.obstacleMap[cx, cy + 1] == null)
            {
                updOpen(cx, cy, 0, 1);
                if (cx + 1 <= max_x && myMind.obstacleMap[cx + 1, cy + 1] == null) updOpen(cx, cy, 1, 1);
                if (cx - 1 >= min_x && myMind.obstacleMap[cx - 1, cy + 1] == null) updOpen(cx, cy, -1, 1);
            }
            if (cy - 1 >= min_x && myMind.obstacleMap[cx, cy - 1] == null)
            {
                updOpen(cx, cy, 0, -1);
                if (cx + 1 <= max_x && myMind.obstacleMap[cx + 1, cy - 1] == null) updOpen(cx, cy, 1, -1);
                if (cx - 1 >= min_x && myMind.obstacleMap[cx - 1, cy - 1] == null) updOpen(cx, cy, -1, -1);
            }
            return buildBackMaps(target);
        }

        private bool next_to(int x, int y, AliveObject target)
        {
            if (dogTricks.distance(x, y, target.X, target.Y) < 2) return true;
            int dx = Math.Sign(target.X - x);
            int dy = Math.Sign(target.Y - y);
            if (myMind.obstacleMap[x + dx, y + dy] == target) return true;
            return false;
        }

        public walkPath(float xc, float yc, float xt, float yt, DogsMind mind)
        {
            min_x = mind.min_x;
            min_y = mind.min_y;
            max_x = mind.max_x;
            max_y = mind.max_y;
            myMind = mind;
            int[] node;
            path = new ArrayList();
            open = new ArrayList();
            backmapX = new int[256, 256];
            backmapY = new int[256, 256];
            myMind.freeGridPoint(xc, yc, out start_x, out start_y);
            myMind.freeGridPoint(xt, yt, out end_x, out end_y);
            if (start_x < min_x || start_y < min_y || end_x < min_x || end_y < min_y ||
                start_x > max_x || start_y > max_y || end_x > max_x || end_y > max_y) return;
            if (start_x == end_x && start_y == end_y) return;
            backmapX[start_x, start_y] = start_x;
            backmapY[start_x, start_y] = start_y;
            node = new int[2] { start_x, start_y };
            open.Add(node);
            if (buildBackMaps() == false) return; // can't get there
            buildPath();
        }


        private bool buildBackMaps()
        {
            if (open.Count == 0) return false;
            int[] c_node = (int[])open[0];
            int cx = c_node[0];
            int cy = c_node[1];
            if (cx == end_x && cy == end_y) return true;
            open.RemoveAt(0);
            if (cx + 1 <= max_x && myMind.obstacleMap[cx + 1, cy] == null)
            {
                updOpen(cx, cy, 1, 0);
                if (cy + 1 <= max_y && myMind.obstacleMap[cx+1,cy+1] == null) updOpen(cx,cy,1,1);
                if (cy -1 >= min_y && myMind.obstacleMap[cx+1,cy-1] == null) updOpen(cx,cy,1,-1);
            }
            if (cx - 1 >= min_x && myMind.obstacleMap[cx + 1, cy] == null)
            {
                updOpen(cx, cy, -1, 0);
                if (cy + 1 <= max_y && myMind.obstacleMap[cx - 1, cy + 1] == null) updOpen(cx, cy, -1, 1);
                if (cy - 1 >= min_y && myMind.obstacleMap[cx - 1, cy - 1] == null) updOpen(cx, cy, -1, -1);
            }
            if (cy + 1 <= max_x && myMind.obstacleMap[cx, cy + 1] == null)
            {
                updOpen(cx, cy, 0, 1);
                if (cx + 1 <= max_x && myMind.obstacleMap[cx + 1, cy + 1] == null) updOpen(cx, cy, 1, 1);
                if (cx - 1 >= min_x && myMind.obstacleMap[cx - 1, cy + 1] == null) updOpen(cx, cy, -1, 1);
            }
            if (cy - 1 >= min_x && myMind.obstacleMap[cx, cy - 1] == null)
            {
                updOpen(cx, cy, 0, -1);
                if (cx + 1 <= max_x && myMind.obstacleMap[cx + 1, cy - 1] == null) updOpen(cx, cy, 1, -1);
                if (cx - 1 >= min_x && myMind.obstacleMap[cx - 1, cy - 1] == null) updOpen(cx, cy, -1, -1);
            }
            return buildBackMaps();
        }

        private void updOpen(int cx, int cy, int dx, int dy)
        {
            if (backmapX[cx + dx, cy + dy] > 0) return; // been there
            int c = open.Count;
            int[] nd;
            int[] node = new int[2] { cx + dx, cy + dy };
            float dist = distance(cx + dx, cy + dy, end_x, end_y); ;
            float d;
            backmapX[cx + dx, cy + dy] = cx;
            backmapY[cx + dx, cy + dy] = cy;
            if (c == 0)
            {
                open.Add(node);
                return;
            }
            for (int i = 0; i < c; i++)
            {
                nd = (int[])open[i];
                d = distance(nd[0], nd[1], end_x, end_y);
                if (dist <= d)
                {
                    open.Insert(i, node);
                    return;
                }
            }
            open.Add(node);
        }

        private float distance(int x1, int y1, int x2, int y2)
        {
            return (float)Math.Sqrt((float)((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2)));
        }

        private void buildPath()
        {
            int[] nd;
            int x, y, nx, ny;
            if (path.Count == 0)
            {
                path.Add(new int[2] { end_x, end_y });
            }
            for (int i = 0; i < 300; i++)
            {
                nd = (int[])path[0];
                x = nd[0];
                y = nd[1];
                if (x == start_x && y == start_y) return;
                nx = backmapX[x, y];
                ny = backmapY[x, y];
                path.Insert(0, new int[2] { nx, ny });
            }
        }

        public bool followPath()
        {
            return followPath(50);
        }

        public bool followPath(int lim)
        {
            int x, y, t;
            float cx, cy, dist, tf;
            bool res = true;
            x = -1;
            y = -1;
            int count = 1;
            foreach (int[] nd in path)
            {
                x = nd[0];
                y = nd[1];
                if (myMind.exploredMap[x, y] == false) 
                {
                    res = false;
                    Console.WriteLine("Hit an unexplored grid point");
                    break; 
                }
                myMind.myDog.Coordinates(out cx, out cy);
                dist = myMind.distance(cx, cy, (float)x, (float)y);
                if (dist > 2.5) 
                { 
                    res = false;
                    Console.WriteLine("Veered off the path");
                    break; 
                } //I am lost
                tf = dist * 320;
                t = (int)Math.Round(tf);
                if (t > 0)
                {
                    myMind.myDog.TurnTo(x, y);
                    myMind.myDog.WalkForward(t);
                }
                if (count >= lim) break;
            }
            myMind.myDog.Coordinates(out cx, out cy);
            Console.WriteLine("Followed path, lim = " + lim.ToString() + " path = " + this.ToMessage(path));
            Console.WriteLine("Ended up at: " + "<" + cx.ToString() + "," + cy.ToString() + ">");
            return res;
        }

        public string ToMessage(ArrayList path)
        {
            string res = "";
            foreach (int[] x in path)
            {
                res = res + " <" + x[0].ToString() + "," + x[1].ToString() + ">";
            }
            return res;
        }

    }
}
