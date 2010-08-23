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

    public class Context
    {
        public float start_pos_x, start_pos_y, start_angle; //dog's position at the start of each task
        public CD last_command;
        public AliveObject last_focus;
        public List<AliveObject> new_objects;
        public AliveObject carried_object = null;
    }
}
