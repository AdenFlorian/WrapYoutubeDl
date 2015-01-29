# WrapYoutubeDl
C# wrapper for https://github.com/rg3/youtube-dl 

# Installation
You need to download or use the binaries in the repo, following exe files are needed;

ffmpeg.exe  - https://www.ffmpeg.org/download.html
youtube-dl.exe   - https://rg3.github.io/youtube-dl/download.html

# Setup

under your <appSettings> please add a key/value pair for your binaries path.
  <appSettings>
    <add key="binaryfolder" value="c:/@Code/WrapYoutubeDl/Binaries/"/>
  </appSettings>
  
# Usage
```
  static void Main(string[] args)
  {
    var urlToDownload = "https://www.youtube.com/watch?v=qDc_5zpBj7s";
    var newFilename = Guid.NewGuid().ToString();
    var mp3OutputFolder = "c:/@mp3/";

    var downloader = new AudioDownloader(urlToDownload, newFilename, mp3OutputFolder);
    downloader.ProgressDownload += downloader_ProgressDownload;
    downloader.FinishedDownload += downloader_FinishedDownload;
    downloader.Download();

    Console.ReadLine();
  }

  static void downloader_FinishedDownload(object sender)
  {
    Console.WriteLine("Finished!");
  }

  static void downloader_ProgressDownload(object sender, ProgressEventArgs e)
  {
    Console.WriteLine(e.Percentage);
  }
```
