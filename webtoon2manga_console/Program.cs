using CommandLine;
using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using webtoon2manga_console.Bindings;
using webtoon2manga_console.Graphics;
using webtoon2manga_console.Tools;

namespace webtoon2manga_console
{

    class SharedOptions
    {
        [Option('f', "file", HelpText = "Input files")]
        public IEnumerable<string> Files { get; set; }

        [Option('d', "dir", HelpText = "Input Folders")]
        public IEnumerable<string> Folders { get; set; }

        [Option('o', "out", HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        // Default true is issue: https://github.com/commandlineparser/commandline/issues/290
        //      Fix: Use bool?
        [Option('p', "pause", Default = false, HelpText = "Pause before exit? ")]
        public bool Pause { get; set; }

        [Option('m', "memory", Default = 1024, HelpText = "Limit Memory (MB)")]
        public int LimitMB { get; set; }
    }

    [Verb("color",HelpText ="Convert page to grayscale and remove black backfround")]
    class ColorOptions : SharedOptions
    {
        [Option("pencil",Default = false, HelpText ="Use pencil tile as background? ")]
        public bool UsePencilTile { get; set; }

    }

    [Verb("duplex", HelpText = "Convert long webtoon strip to A4 double side")]
    class DuplexOptions : SharedOptions
    {
        [Option('l',"landscape", Default = true, HelpText = "Landscape? ")]
        public bool? Landscape { get; set; }

        [Option('c',"col",Required =true, HelpText = "How much columns to put in a page")]
        public int Columns { get; set; }
    }

    class Program
    {
        static LoggerHelper log = new LoggerHelper("MAIN");

        static void Main(string[] args)
        {
            var args_parsed = Parser.Default.ParseArguments<ColorOptions, DuplexOptions>(args)
                .WithParsed<ColorOptions>((options) =>
                {
                    ImageMagick.ResourceLimits.Memory = (ulong)options.LimitMB * (ulong)Math.Pow(1024, 2); // 2=MB, 3=GB ...

                    Color(options);

                    if (options.Pause)
                        Pause();
                })
                .WithParsed<DuplexOptions>((options) =>
                {
                    ImageMagick.ResourceLimits.Memory = (ulong)options.LimitMB * (ulong)Math.Pow(1024, 2); // 2=MB, 3=GB ...

                    Duplex(options);

                    if (options.Pause)
                        Pause();
                })
                .WithNotParsed(errors =>
                {
                    log.e("Error parsing inputs");
                    foreach(var e in errors)
                    {
                        log.e(e);
                    }
                });
        }

        static void Pause()
        {
            Console.WriteLine("Enter to exit...");
            Console.ReadLine();
        }

        delegate void fileProcess(string file, string output_file, LoggerHelper log);
        static void ProcessFile(string tag, string file, string outputFolder, fileProcess callback)
        {
            LoggerHelper log = new LoggerHelper(tag);
            try
            {
                FileInfo _fi = new FileInfo(file);
                if (_fi.Exists)
                {
                    string outputFile = file;
                    if (!string.IsNullOrEmpty(outputFolder))
                        outputFile = _fi.FullName.Replace(_fi.DirectoryName, outputFolder);
                    var _fi_out = new FileInfo(outputFile);
                    if (!_fi_out.Directory.Exists)
                    {
                        log.i("Creating: " + _fi_out.Directory.FullName);
                        _fi_out.Directory.Create();
                    }
                    log.i("Start processing '" + _fi.FullName + "'");
                    callback(file, outputFile,log);
                    log.i("Done '" + _fi.Name + "'");
                }
                else
                {
                    log.e("Can't find file '" + file + "', skipping");
                }
            }
            catch (Exception ex)
            {
                log.e("Fail processing file '" + file + "'", ex);
            }
        }

        private static void ProcessAllFiles(SharedOptions opt, Action<string> _file_job)
        {
            var files = opt.Files.ToArray();
            log.i(LoggerHelper.Stringify("Files", files));
            foreach (string f in files)
            {
                _file_job(f);
            }

            var dirs = opt.Folders.ToArray();
            log.i(LoggerHelper.Stringify("Folders", dirs));
            foreach (string d in dirs)
            {
                DirectoryInfo _di = new DirectoryInfo(d);
                if (_di.Exists)
                {
                    foreach (FileInfo fi in _di.GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        _file_job(fi.FullName);
                    }
                }
                else
                {
                    log.e("Can't find dir '" + d + "', skipping");
                }
            }
        }

        static void Color(ColorOptions opt)
        {
            Action<string> _file_job = new Action<string>((string input_file) =>
            {
                ProcessFile("color-convert", input_file, opt.OutputFolder, (file, outfile, log) =>
                {
                    using (var fileResult = RemoveBlack.FromFile(file, opt.UsePencilTile))
                    {
                        fileResult.Write(outfile);
                    }
                });
            });

            ProcessAllFiles(opt, _file_job);
        }

       

        static void Duplex(DuplexOptions opt)
        {
            Duplex d = new Duplex(TemplatesTools.getA4(150,!(opt.Landscape ?? true)),opt.Columns);
            List<PageFragmnet> fragments = new List<PageFragmnet>();

            Action<string> _file_job = new Action<string>((string input_file) =>
            {
                 ProcessFile("duplex-job", input_file, opt.OutputFolder, (file, output_file,log) =>
                 {
                     log.i(string.Format("Source: {0}\nTarget: {1}", file, output_file));
                     var toon = new WebtoonPage()
                     {
                         filpath = input_file
                     };
                     using (MagickImage img = new MagickImage(file))
                     {
                         toon.height = img.Height;
                         toon.width = img.Width;
                     }

                     fragments.AddRange(d.splitPageLandscape(toon));
                 });
            });

            ProcessAllFiles(opt, _file_job);

            d.saveCahpterFragmentsIntoPNG_LTR(fragments, "", opt.OutputFolder);
        }
    }
}
