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
            LoginButton.Click       += new EventHandler(LoginButton_Click);
            LogoutButton.Click      += new EventHandler(LogoutButton_Click);
            MoveForwardButton.Click += new EventHandler(MoveForwardButton_Click);
        }

        private void LoginButton_Click(object sender, EventArgs e)
        {
            Fido = new SmartDog("Dog", NameBox.Text, "alive");
            bool success = Fido.Login();

            MessageBox.AppendText("Login: " + success);
        }

        private void LogoutButton_Click(object sender, EventArgs e)
        {
            if (Fido != null)
                Fido.Logout();
        }

        private void MoveForwardButton_Click(object sender, EventArgs e)
        {
            if (Fido != null)
            {
                bool success = Fido.GoForward(1);
                MessageBox.AppendText("GoForward: " + success);
            }
        }
        
        }
}
