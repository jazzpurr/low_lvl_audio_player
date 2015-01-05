using System;
using System.Collections.Generic;
using System.Text;
using NAudio.Wave;
using System.Collections;
using PortAudioSharp;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace PortAudioSharpTest
{
    /// <summary>
    /// Wave file class
    /// 
    /// I'm thinking that maybe this can encapsulate all file IO and buffering that we may need to do
    /// so that all that we need to do to get more samples is call wavFile.read(n) to get n samples.
    /// </summary>
    class wavFile
    {


        Random newRand = new Random();
        private const int NUM_SAMPLES = 4096/8;
        private const int QUEUE_LENGTH = 40;
        private bool stop_flag = false;
        private bool pause_flag = false;

        public string filename;
        public int inputChannels;
        public int outputChannels;
        public int bitDepth;
        public int frameSize; //for 16 bit stereo, it would be 2 bytes * 2 channels = 4 bytes
        public int sampleRate;
        public long numOfSamples;
        public long numOfFrames;
        public long cSamplePos; // in bytes
        public bool isPlaying = false;

        private WaveFileReader reader;
        private Queue sampleQueue;
        private Form1 form;

        private IntPtr stream;


        PortAudio.PaStreamCallbackDelegate callbackDelegate; //kept as a class variable so it doesn't get garbage collected

        /// <summary>
        /// - A public int to count the current sample we're up to so that we're able to 'fast forward' the audio by increasing the current sample count.
        /// - A sample queue, same as what I'm using now.
        /// </summary>

        //private file

        public wavFile(string filename, Form1 form)
        {
            this.filename = filename;
            reader = new WaveFileReader(this.filename);
            sampleQueue = new Queue();
            this.form = form;
            this.inputChannels = reader.WaveFormat.Channels;
            this.outputChannels = 2;
            this.bitDepth = reader.WaveFormat.BitsPerSample;
            this.frameSize = reader.WaveFormat.BlockAlign;
            this.sampleRate = reader.WaveFormat.SampleRate;
            this.numOfSamples = reader.SampleCount;
            this.numOfFrames = this.numOfSamples / this.frameSize;
            this.cSamplePos = 0;

            Console.WriteLine(this.sampleRate + " " + this.bitDepth + " " + this.inputChannels);
            //reader.Seek((int)startOffset*this.sampleRate, System.IO.SeekOrigin.Begin);
        }

        public void set_offset(int offset)
        {
            reader.CurrentTime = new TimeSpan(0, 0, 0);
            reader.CurrentTime = reader.CurrentTime.Add(new TimeSpan(0, 0, 0, offset));
        }

        public void set_sample_rate(int sampleRate)
        {
            this.sampleRate = sampleRate;
        }

        public void Stop()
        {
            stop_flag = true;
            sampleQueue.Clear();
        }

        public void Pause()
        {
            pause_flag = true;
        }

        public void Unpause()
        {
            pause_flag = false; 
        }
        
        private void play_loop()
        {
            while (reader.Position < reader.Length)
            {
                if (stop_flag)
                {
                    break;
                }

                if (pause_flag)
                {
                    continue; //just wait for it to be unpaused
                }

                //This is essentially working as a circular/FIFO buffer, where new sample packets are only added to the queue
                //if there's room in the queue. Once the packet is read out of the queue in the callback function it's 
                //removed from the queue and there is room to add more info to the queue
                if (sampleQueue.Count < QUEUE_LENGTH)
                {
                    //Console.WriteLine("Writing");

                    byte[] buffer = new byte[NUM_SAMPLES]; //buffer to read the wav raw bytes into

                    int bytesRead = reader.Read(buffer, 0, NUM_SAMPLES); //read a block of bytes out from the wav

                    cSamplePos += bytesRead;
                    
                    SoundPacket packet = new SoundPacket(buffer);
                    
                    sampleQueue.Enqueue(packet); //send the buffer to the queue
                }

            }

            this.stop_flag = true;

            while (PortAudio.Pa_IsStreamActive(stream) != 0)
            {
                
            }

            PortAudio.Pa_StopStream(stream);
            this.isPlaying = false;
            cSamplePos = 0;
            this.stop_flag = false;

        }

        public void Play()
        {
            IntPtr userdata = IntPtr.Zero; //intptr.zero is essentially just a null pointer
            callbackDelegate = new PortAudio.PaStreamCallbackDelegate(myPaStreamCallback);
            PortAudio.Pa_Initialize();

            uint sampleFormat = 0;
            
            switch (bitDepth)
            {
                case 8:
                    sampleFormat = 16;
                    break;
                case 16:
                    sampleFormat = 8;
                    break;
                case 24:
                    sampleFormat = 4;
                    break;
                case 32:
                    sampleFormat = 2;
                    break;
                default:
                    Console.WriteLine("broken WAV");
                    break;

            }
            //not sure why framesPerBuffer is so strange.
            PortAudio.PaError error = PortAudio.Pa_OpenDefaultStream(out stream, inputChannels, outputChannels, sampleFormat,
                sampleRate / outputChannels, (uint)(NUM_SAMPLES / (frameSize * 2)), callbackDelegate, userdata);

            PortAudio.Pa_StartStream(stream);

            Thread myThread = new Thread(new ThreadStart(play_loop));
            myThread.Start();
        }

        public PortAudio.PaStreamCallbackResult myPaStreamCallback(
            IntPtr input,
            IntPtr output,
            uint frameCount,
            ref PortAudio.PaStreamCallbackTimeInfo timeInfo,
            PortAudio.PaStreamCallbackFlags statusFlags,
            IntPtr userData)
        {

            SoundPacket packet;

            if (stop_flag && sampleQueue.Count == 0)
            {
                //this is likely a bad way of checking if the stream is complete as it could get in here if the read thread
                //falls behind but it works behind. 
                //todo: find a smarter way of knowing when the stream completes

                return PortAudio.PaStreamCallbackResult.paComplete;
            }

            if (sampleQueue.Count == 0)
            {
                byte[] buffer = new byte[NUM_SAMPLES];
                Marshal.Copy(buffer, 0, output, (int)(frameCount * (bitDepth / 8) * 2));

                return PortAudio.PaStreamCallbackResult.paContinue; //probably paused
            }

            packet = (SoundPacket)sampleQueue.Dequeue();

            form.Invoke(form.myDelegate, new object[] {packet.averageDB});
            
            Marshal.Copy(packet.samples, 0, output, (int)(frameCount * (bitDepth / 8) * 2));

            return PortAudio.PaStreamCallbackResult.paContinue;
        }

    }

    class SoundPacket
    {
        public byte[] samples;
        public int averageDB;
        const int minDB = 91; // == 20*log10(1/short.MaxValue)
        const double maxShort = (double)short.MaxValue;
        public short[] s_samples;
            
        public SoundPacket(byte[] samples)
        {
            this.samples = samples;
            this.averageDB = getAverageDB();
        }

        private int getAverageDB()
        {
            s_samples = new short[samples.Length / 2];
            Buffer.BlockCopy(samples, 0, s_samples, 0, samples.Length); //copy them to a buffer of samples

            int total = 0;

            for (int i = 0; i < s_samples.Length; i++)
            {
                int sample = s_samples[i];
                total += Math.Abs(sample);
            }

            int average = total / s_samples.Length;

            if (average == 0)
                average = 1;

            double db = 20*Math.Log10(average / maxShort) + minDB;

            return (int)db;
        }
    }


}


