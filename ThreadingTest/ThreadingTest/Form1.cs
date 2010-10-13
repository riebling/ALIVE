using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace GUIthreadExample
{
    public partial class Form1 : Form
    {
        readonly object lockObject = new object();
        private bool stopFlag = false;
        delegate void writeTextDelegate(string text);
        delegate void resetButtonsDelegate();
        
        public Form1()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            startButton.Enabled = false;
            stopFlag = false;
            Thread t = new Thread(new ThreadStart(ThreadJob));
            t.IsBackground = true;
            t.Start();
        }

        private void ThreadJob()
        {
            bool localStopFlag = false;
            for (int i = 1; i <= 1000 && localStopFlag == false; i++)
            {
                writeNumber(i.ToString());
                Thread.Sleep(250);
                lock (lockObject)
                {
                    localStopFlag = stopFlag;
                }
            }
            resetButtons();
        }

        public void writeNumber(string text)
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new writeTextDelegate(writeNumber), new object[] { text });
                return;
            }
            lock (lockObject)
            {
                textBox1.Text = text;
            }
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            stopButton.Enabled = false;
            lock (lockObject)
            {
                stopFlag = true;
            }
        }

        public void resetButtons()
        {
            if (this.InvokeRequired)
            {
                BeginInvoke(new resetButtonsDelegate(resetButtons));
                return;
            }
            lock (lockObject)
            {
                stopButton.Enabled = true;
                startButton.Enabled = true;
                stopFlag = false;
                textBox1.Text = "";
            }
        }
    }
}
