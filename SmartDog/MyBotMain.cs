using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace MyBot.MyBotMain
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new BotControlForm1());
            }
            catch(System.Exception ex)
            {
                Console.WriteLine("MAIN NASTY: " + ex.ToString());
            }
        }
    }
}