using Carbon.Csv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace videoconvert
{
    class Program
    {
        public static string Extension = "mp4";
        public static string ConfigFile = "videos.csv";
        public static string VideoNameTemplate = "v{0}." + Extension;
        public static string VideoIntroNameTemplate = "intro_{0}." + Extension;

        static void Main(string[] args)
        {
            var reader = new StreamReader(Path.Combine(ConfigFile));
            var config = new CsvReader(reader, ';');

            ConvertProcessCreator.OutputPrefix = "udemy_";

            var cpc = new ConvertProcessCreator(CreateConcatFile, GetOutputFileName);

            var v = new VideoInfoAdapter(GetIntroName, GetVideoName);

            IReadOnlyList<String> line;

            while (!reader.EndOfStream && (line = config.ReadRow()) != null)
            {
                Process process = cpc.Create(v.GetVideosToConcat(line));

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit(5000);
            }

            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        private static string CreateConcatFile(IList<String> filesToConcat)
        {
            Func<String, String> skipExt = (arg) =>
            {
                var fi = new FileInfo(arg);
                return fi.Name.Replace(fi.Extension, String.Empty);
            };

            var name = filesToConcat.Aggregate<String>((arg0, arg1) => string.Format("{0}_{1}", skipExt(arg0), skipExt(arg1)));
            var concatName = string.Format("{0}.txt", name);
            StreamWriter concat = new StreamWriter(concatName);

            
            foreach (var file in filesToConcat)
            {
                concat.WriteLine(string.Format("file '{0}'", file));
            }
            concat.Close();

            return concatName; 
        }

        private static string GetOutputFileName(IList<String> filesToConcat)
        {
            return filesToConcat.Last<String>(); 
        }

        private static string GetIntroName(int _VideoType)
        {
            var introNames = new List<String>() { "practica", "teoria", "herramienta", "plan" };

            return string.Format(VideoIntroNameTemplate, introNames[_VideoType]); 
        }

        private static string GetVideoName(int _Order)
        {
            return string.Format(VideoNameTemplate, _Order); 
        }

        public class VideoInfoAdapter
        {
            public static int Column_Order = 0;
            public static int Column_Type = 1;


            private Func<int, String> GetIntroName;
            private Func<int, String> GetVideoName;

            public VideoInfoAdapter(
                Func<int, String> _GetIntroName, 
                Func<int, String> _GetVideoName)
            {


                GetIntroName = _GetIntroName;
                GetVideoName = _GetVideoName;
            }

            public List<String> GetVideosToConcat(IReadOnlyList<string> columns)
            {
                int Type;
                int Order;

                Order = Int32.Parse(columns[Column_Order]);
                Type = Int32.Parse(columns[Column_Type]);

                var videoName = GetVideoName(Order);
                var videoIntro = GetIntroName(Type);

                return new List<String>() { videoIntro, videoName };
            }


        }
        public class ConvertProcessCreator
        {
            public static string OutputPrefix = "_";
            public static string ConcatParameters = " -f concat -safe 1 -i {0} -c copy {1}{2}";

            private Func<IList<String>, String> createConcatFile;
            private Func<IList<String>, String> getOutputFile;

            public ConvertProcessCreator( 
                Func<IList<String>, String> _createConcatFile,
                Func<IList<String>, String> _getOutputFile)
            {
                
                createConcatFile = _createConcatFile;
                getOutputFile = _getOutputFile;
            }

            public Process Create(IList<String> _videos)
            {
                IList<String> videos = _videos;

                var concatFileName = createConcatFile(videos);
                var outputFile = getOutputFile(videos);

                var param = string.Format(ConcatParameters, concatFileName, OutputPrefix, outputFile);

                var cmd_ffmepg = new ProcessStartInfo("ffmpeg.exe", param);

                // redirect the output
                cmd_ffmepg.RedirectStandardOutput = true;
                cmd_ffmepg.RedirectStandardError = true;
                cmd_ffmepg.UseShellExecute = false;
                var sb = new StringBuilder();

                cmd_ffmepg.ErrorDialog = true;

                var process = new Process();

                process.OutputDataReceived += (sender, args1) => Console.WriteLine(args1.Data);
                process.ErrorDataReceived += (sender, args1) => Console.WriteLine(args1.Data);

                process.StartInfo = cmd_ffmepg;

                return process;
            }
        }
                                                                 
    }
}
