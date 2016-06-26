using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WrapYoutubeDl {
    public static class youtube_dl {
        public static Object ProcessObject { get; set; }
        public static bool Started { get; set; }
        public static bool Finished { get; set; }
        public static decimal Percentage { get; set; }
        public static Process Process { get; set; }

        public static string ConsoleLog { get; set; }

        public static string resultVideoID { get; private set; }
        
        public static string GetVideoID(string searchString) {
            Started = false;
            Finished = false;
            Percentage = 0;

            // this is the path where you keep the binaries (ffmpeg, youtube-dl etc)
            var binaryPath = ConfigurationManager.AppSettings["binaryfolder"];
            if (string.IsNullOrEmpty(binaryPath)) {
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");
            }
            
            var arguments = string.Format("--get-id \"ytsearch1:{0}\"", searchString);  //--ignore-errors

            var fullPathToEXE = System.IO.Path.Combine(binaryPath, "youtube-dl.exe"); ;

            // setup the process that will fire youtube-dl
            Process = new Process();
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.RedirectStandardOutput = true;
            Process.StartInfo.RedirectStandardError = true;
            Process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            Process.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(fullPathToEXE);
            Process.StartInfo.FileName = System.IO.Path.GetFileName(fullPathToEXE);
            Process.StartInfo.Arguments = arguments;
            Process.StartInfo.CreateNoWindow = false;
            Process.EnableRaisingEvents = true;

            Process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            Process.ErrorDataReceived += new DataReceivedEventHandler(ErrorDataReceived);

            return Download();
        }

        static string Download() {
            Console.WriteLine("Downloading");
            Process.Exited += Process_Exited;

            Console.WriteLine("\n" + Process.StartInfo.FileName + " " + Process.StartInfo.Arguments + "\n");

            Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
            Console.Write("Waiting for Process to exit...");
            // Wait for the child app to stop
            Process.WaitForExit();
            Console.WriteLine("Exited!");

            return resultVideoID;
        }

        static void Process_Exited(object sender, EventArgs e) {
            Console.WriteLine("youtube-dl Exited");
        }

        public static void ErrorDataReceived(object sendingprocess, DataReceivedEventArgs error) {
            Console.WriteLine(error.Data);
        }

        public static void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
            if (String.IsNullOrEmpty(outLine.Data)) {
                return;
            }

            Console.WriteLine(outLine.Data);

            resultVideoID = outLine.Data;
        }
    }
}
