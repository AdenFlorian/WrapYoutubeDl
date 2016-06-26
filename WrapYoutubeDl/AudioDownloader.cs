using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WrapYoutubeDl
{
    public class AudioDownloader
    {
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public event ProgressEventHandler ProgressDownload;

        public delegate void FinishedDownloadEventHandler(object sender, DownloadEventArgs e);
        public event FinishedDownloadEventHandler FinishedDownload;

        public delegate void StartedDownloadEventHandler(object sender, DownloadEventArgs e);
        public event StartedDownloadEventHandler StartedDownload;

        public delegate void ErrorEventHandler(object sender, ProgressEventArgs e);
        public event ErrorEventHandler ErrorDownload;

        public Object ProcessObject { get; set; }
        public bool Started { get; set; }
        public bool Finished { get; set; }
        public decimal Percentage { get; set; }
        public Process Process { get; set; }
        public string OutputName { get; set; }
        public string DestinationFolder { get; set; }
        public string Url { get; set; }

        public string ConsoleLog { get; set; }

        public string finishedOutputFilePath { get; private set; }


        public AudioDownloader(string url, string outputName, string outputfolder)
        {
            this.Started = false;
            this.Finished = false;
            this.Percentage = 0;

            DestinationFolder = outputfolder;
            Url = url;

            // make sure filename ends with an mp3 extension
            OutputName = outputName;
            if (!OutputName.ToLower().EndsWith(".mp3"))
            {
                OutputName += ".mp3";
            }

            // this is the path where you keep the binaries (ffmpeg, youtube-dl etc)
            var binaryPath = ConfigurationManager.AppSettings["binaryfolder"];
            if (string.IsNullOrEmpty(binaryPath))
            {
                throw new Exception("Cannot read 'binaryfolder' variable from app.config / web.config.");
            }

            // if the destination file exists, exit
            //var destinationPath = System.IO.Path.Combine(outputfolder, OutputName);
            //if (System.IO.File.Exists(destinationPath))
            //{
            //    throw new Exception(destinationPath + " exists");
            //}
            var arguments = string.Format(@"--max-filesize 50m --extract-audio {0} -o {1}%(id)s.%(ext)s", url, outputfolder);  //--ignore-errors

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


        }

        protected virtual void OnProgress(ProgressEventArgs e)
        {
            if (ProgressDownload != null)
            {
                ProgressDownload(this, e);               
            }
        }

        protected virtual void OnDownloadFinished(DownloadEventArgs e)
        {
            if (Finished == false)
            {
                Finished = true;
                FinishedDownload?.Invoke(this, e);
            }
        }

        protected virtual void OnDownloadStarted(DownloadEventArgs e)
        {
            StartedDownload?.Invoke(this, e);
        }

        protected virtual void OnDownloadError(ProgressEventArgs e) {
            ErrorDownload?.Invoke(this, e);
        }

        public string Download()
        {



            Console.WriteLine("Downloading {0}", Url);
            Process.Exited += Process_Exited;

            Console.WriteLine("\n" + Process.StartInfo.FileName + " " + Process.StartInfo.Arguments + "\n");

            Process.Start();
            Process.BeginOutputReadLine();
            Process.BeginErrorReadLine();
            Console.Write("Waiting for Process to exit...");
            // Wait for the child app to stop
            Process.WaitForExit();
            Console.WriteLine("Exited!");

            return finishedOutputFilePath;
        }

        void Process_Exited(object sender, EventArgs e) {
            Console.WriteLine("youtube-dl Exited");
            OnDownloadFinished(new DownloadEventArgs() { ProcessObject = this.ProcessObject });
        }

        public void ErrorDataReceived(object sendingprocess, DataReceivedEventArgs error) {
            Console.WriteLine(error.Data);
            if (!String.IsNullOrEmpty(error.Data))
            {
                this.OnDownloadError(new ProgressEventArgs() { Error = error.Data, ProcessObject = this.ProcessObject });
            }
        }
        public void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {

            // extract the percentage from process output
            if (String.IsNullOrEmpty(outLine.Data)) {
                return;
            }
            Console.WriteLine(outLine.Data);

            var ffmpegDestinationString = "[ffmpeg] Destination: ";
            var ffmpegCorrectingContainerString = "[ffmpeg] Correcting container in \"";
            if (outLine.Data.StartsWith(ffmpegDestinationString)) {
                finishedOutputFilePath = outLine.Data.Substring(ffmpegDestinationString.Length);
            } else if (outLine.Data.StartsWith(ffmpegCorrectingContainerString)) {
                finishedOutputFilePath = outLine.Data.Substring(ffmpegCorrectingContainerString.Length).TrimEnd('"');
            }

            // extract the percentage from process output
            if (Finished) {
                return;
            }

            this.ConsoleLog += outLine.Data;

            if (outLine.Data.Contains("ERROR"))
            {
                this.OnDownloadError(new ProgressEventArgs() { Error = outLine.Data, ProcessObject = this.ProcessObject });
                return;
            }

            if (!outLine.Data.Contains("[download]"))
            {
                return;
            }
            var pattern = new Regex(@"\b\d+([\.,]\d+)?", RegexOptions.None);
            if (!pattern.IsMatch(outLine.Data))
            {
                return;
            }

            // fire the process event
            var perc = Convert.ToDecimal(Regex.Match(outLine.Data, @"\b\d+([\.,]\d+)?").Value);
            if (perc > 100 || perc < 0)
            {
                Console.WriteLine("weird perc {0}", perc);
                return;
            }
            this.Percentage = perc;
            this.OnProgress(new ProgressEventArgs() { ProcessObject = this.ProcessObject, Percentage = perc });

            // is it finished?
            if (perc < 100)
            {
                return;
            }

            if (perc == 100 && !Finished)
            {
                OnDownloadFinished(new DownloadEventArgs() { ProcessObject = this.ProcessObject });
            }
        }
    }

}
