using System;
using System.Reflection;

using PortAudioSharp;
using System.Drawing;
using System.Windows.Forms;

namespace PortAudioSharpTest
{

	class MainClass
	{
		[STAThread]
		public static void Main()
		{
            Application.EnableVisualStyles();
            Application.Run(new Form2());
            Application.Run(new Form1());

			Console.WriteLine("PortAudioSharp Test");
			Console.WriteLine("*******************");
			Console.WriteLine();
			Console.WriteLine("PortAudioSharpTest version: "
				+ Assembly.GetExecutingAssembly().GetName().Version.ToString());
			Console.WriteLine("PortAudioSharp version: "
				+ Assembly.GetAssembly(typeof(PortAudio)).GetName().Version.ToString());
			Console.WriteLine("PortAudio version: " 
				+ PortAudio.Pa_GetVersionText() 
				+ " (" + PortAudio.Pa_GetVersion() + ")");
			Console.WriteLine(); 
			
			new PortAudioTest().Run();
		}
	}

}
