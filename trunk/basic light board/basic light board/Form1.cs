﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace basic_light_board
{
    /// <summary>
    /// this form provides a basic 24 channel 1 to 1 patched XY cross fader.
    /// it allows for timed fades using a go button or manual time control using a crossfader.
    /// this currently does not interface with the Enttec USB pro because i havent gotten the thing yet
    /// if all goes well it should work just fine.
    /// </summary>
    public partial class Form1 : Form
    {
        public const int universeSize=512;
        byte[] LiveLevels = new byte[universeSize];
        byte[] XLevels = new byte[universeSize];
        byte[] YLevels = new byte[universeSize];
        output m_outForm;
        Stopwatch m_timer;

        //int iterations;
        int change;

        public Form1()
        {
            InitializeComponent();
            m_outForm = new output(48);
            m_outForm.Show();
            
        }

        private void updateTextBox()
        {
            StringBuilder str = new StringBuilder();
            int i = 1;
            foreach (byte b in LiveLevels)
            {
                str.AppendFormat("Ch{0,2}:{1,4} ", i++, b);
                if (i!=0 && (i-1) % 6 == 0) str.AppendLine();
            }
            textBox1.Text = str.ToString();
        }


        private void updateOutForm()
        {
            for (int i = 0; i < m_outForm.m_Bars.Count; i++)
            {
                m_outForm.m_Bars[i].Value = LiveLevels[i+1];
            }
        }


        
        private void FullScale(byte[] Live, byte[] X, byte[] Y, byte xLev, byte yLev)
        {
            int max = universeSize;
            for (int i = 0; i < max; i++)
            {
                Live[i] = scale(X[i], Y[i], xLev,yLev);
            }
            updateTextBox();
            updateOutForm();
        }
               

        private byte scale(byte Xval, byte Yval, byte XLevel,byte Ylevel)
        {
            int xTemp = (int)(Xval * (XLevel / 255.0));
            int yTemp = (int)(Yval * (Ylevel / 255.0));
            int temp =(int)Math.Round(((Xval * (XLevel / 255.0) + Yval * (Ylevel / 255.0))));

            return (byte)(xTemp < yTemp ? yTemp : xTemp);
            //return (byte)(temp > 255 ? 255 : temp);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            FullScale(LiveLevels, sliderGroup1.Values, sliderGroup2.Values, crossFaders1.Scene1Value, crossFaders1.Scene2Value);
            if (sender is CrossFaders )
            {
                if ((sender as CrossFaders).CrossFaderValue == 0) tabControl1.SelectTab(1);
                if ((sender as CrossFaders).CrossFaderValue == 255) tabControl1.SelectTab(0);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Timer goTime = new Timer();
            if ((int)(((int)numericUpDown1.Value)/255.0) == 0)
            {
                crossFaders1.CrossFaderValue = (byte)(crossFaders1.CrossFaderValue == 0 ? 255 : 0);
                return;
            }
            goTime.Interval = (int)((double)numericUpDown1.Value / 255.0);
            goTime.Tick += new EventHandler(goTime_Tick);
            if (crossFaders1.CrossFaderValue == 0) change=  1;//up
            else change = -1;//down
            button1.Enabled = false;
            m_timer = new Stopwatch();
            m_timer.Start();
            
            goTime.Start();
            

        }

        void goTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Timer t = (sender as Timer);
            crossFaders1.CrossFaderValue = (byte)(crossFaders1.CrossFaderValue + change );

            if (crossFaders1.CrossFaderValue == 0 || crossFaders1.CrossFaderValue == 255)
            {
                button1.Enabled = true;
                
                t.Stop();
                m_timer.Stop();
                MessageBox.Show(string.Format("time={0}ms", m_timer.Elapsed));
            }
        }

        void updateFader(byte val)
        {
            crossFaders1.CrossFaderValue = val;
        }

        void goTime_Tick(object sender, EventArgs e)
        {

            Timer t = (sender as Timer);
            if (crossFaders1.InvokeRequired)
                crossFaders1.Invoke( new Action<byte>(updateFader),(byte)(crossFaders1.CrossFaderValue + change));
            else
                updateFader((byte)(crossFaders1.CrossFaderValue + change));

            if (crossFaders1.CrossFaderValue == 0 || crossFaders1.CrossFaderValue == 255)
            {
                button1.Enabled = true;
                t.Stop();
            }
            
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SliderGroup.patch(47, 1, 128);
        }


        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            Regex rx = new Regex(@"(?<dimmer>\d+)(@(?<channel>\d+)(@(?<level>\d+))?)?", RegexOptions.Compiled);
            Regex rx2 = new Regex(@"\d+@\d+@\d+", RegexOptions.Compiled);


            Match m = rx.Match(textBox2.Text);
            //m = rx2.Match(textBox2.Text);
            label1.Text = "dimmer: " + m.Groups["dimmer"].Value;
            label2.Text = "channel: " + m.Groups["channel"].Value;
            label3.Text = "Value: " + m.Groups["level"].Value;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                Regex rx = new Regex(@"(?<dimmer>\d+)(@(?<channel>\d+)(@(?<level>\d+))?)?", RegexOptions.Compiled);
                Match m = rx.Match(textBox2.Text);
                if (!m.Success) { MessageBox.Show("bad string"); return; }

                try
                {
                    int d = int.Parse(m.Groups["dimmer"].Value);
                    int c = int.Parse(m.Groups["channel"].Value);
                    byte l = byte.Parse(m.Groups["level"].Value);

                    SliderGroup.patch(d, c, l);
                    MessageBox.Show(string.Format("Patched dimmer {0} to channel {1} @ {2}", d, c, l));
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException) MessageBox.Show("there was an argument Exception");
                }
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                Regex rx = new Regex(@"(?<channel>\d+)@(?<level>\d+)", RegexOptions.Compiled);
                Match m = rx.Match(textBox3.Text);
                if (!m.Success) { MessageBox.Show("bad string"); return; }

                try
                {
                    int c = int.Parse(m.Groups["channel"].Value);
                    byte l = byte.Parse(m.Groups["level"].Value);

                    if (c < 1 || l < 0 || l > 255) throw new ArgumentException();

                    sliderGroup1.setLevel(c, l);
                    MessageBox.Show(string.Format("channel {0} @ {1}", c, l));
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentException) MessageBox.Show("there was an argument Exception");
                }
            }
        }

    }
}
