using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace PortAudioSharpTest
{
    public partial class Form1 : Form
    {
        public string filename;
        
        public delegate void updateMeter(int value);
        public updateMeter myDelegate;
        private wavFile wav = null;


        
        public Form1()
        {
            InitializeComponent();
            myDelegate = new updateMeter(updateDbMeter);
            progressBar1.Maximum = 100;
            progressBar1.Minimum = 0;
            tb_offset.Text = "0";
            tb_sampleRate.Text = "0";
        }

        private void openWaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (wav != null)
                wav.Stop();
            
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Wave File (*.wav)|*.wav;";
            if (open.ShowDialog() != DialogResult.OK) return;

            filename = open.FileName;

            wav = new wavFile(filename, this);
            tb_sampleRate.Text = wav.sampleRate.ToString();

        }
        

        private void updateDbMeter(int value)
        {
            progressBar1.Value = value;
        }

        private void tb_sampleRate_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && !(e.KeyChar == '.');
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            if (wav != null)
                wav.Stop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (wav != null)
                wav.Stop();

            Thread.Sleep(50); //let the audio thread have a bit of time to die
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (wav != null)
            {
                if (!wav.isPlaying)
                {
                    wav.isPlaying = true;

                    wav.set_sample_rate(Convert.ToInt32(tb_sampleRate.Text));
                    wav.set_offset(Convert.ToInt32(tb_offset.Text));
                    wav.Play();
                }

                wav.Unpause();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (wav != null)
            {
                wav.Pause();
            }
        }

    }
}
