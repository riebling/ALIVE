using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ALIVE;

namespace AliveControlPanel
{
    public partial class Form1 : Form
    {
        public SmartDog Fido = null;

        public Form1()
        {
            // Initialize the GUI 
            InitializeComponent();

            // Register button click callbacks
            LoginButton.Click += new EventHandler(LoginButton_Click);
        }

        private void ParseNames(out string fn, out string ln, out string pw)
        {
            fn = ""; ln = ""; pw = "";
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            string fn, ln, pw;

            ParseNames(out fn, out ln, out pw);
            Fido = new SmartDog(fn, ln, pw);
            if (!Fido.Login())
                MessageBox.Show("Login failed\r\n" + 
                    "Perhaps that user is already logged in?");
        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            if (Fido != null)
                Fido.Logout();
        }

        private void MoveForwardButton_Click(object sender, EventArgs e)
        {
            if (!Fido.GoForward(1))
                MessageBox.Show("Failed to move forward\r\n" +
                    "Perhaps you hit an obstacle?");
        }
        
        }
}
