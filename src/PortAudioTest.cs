using System;
using System.Runtime.InteropServices;

using System.Collections;
using System.IO;

namespace PortAudioSharpTest
{

    public class PortAudioTest
    {

        public PortAudioTest()
        { }


        public void Run()
        {
            string rootPath = new DirectoryInfo(Environment.CurrentDirectory).Parent.Parent.FullName;
            //wavFile wavFile = new wavFile(rootPath + "/wave_files/jazzblues.wav");
            //wavFile.Play();
        }
    }

}