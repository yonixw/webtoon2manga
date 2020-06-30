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
        [Option("orderbyname", Default = true, HelpText = "Shoulde we order by name or time?")]
        public bool? OderByName { get; set; }

        [Option('f', "file", HelpText = "Input files")]
        public IEnumerable<string> Files { get; set; }

        [Option('d', "dir", HelpText = "Input Folders")]
        public IEnumerable<string> Folders { get; set; }

        [Option('o', "out", Required = true, HelpText = "Output folder")]
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

        [Option('z', "fuzz", Default = 1, HelpText = "Fuzz (in %) for finding black color")]
        public int fuzz { get; set; }

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

        static Func<System.IO.FileSystemInfo, object> GlobalOrderFunc = (si) => si.CreationTime;

        static void Main(string[] args)
        {
            var args_parsed = Parser.Default.ParseArguments<ColorOptions, DuplexOptions>(args)
                .WithParsed<ColorOptions>((options) =>
                {
                    ImageMagick.ResourceLimits.Memory = (ulong)options.LimitMB * (ulong)Math.Pow(1024, 2); // 2=MB, 3=GB ...

                    if (options.OderByName ?? true)
                        GlobalOrderFunc = (si) => si.Name;
                    Color(options);

                    if (options.Pause)
                        Pause();
                })
                .WithParsed<DuplexOptions>((options) =>
                {
                    ImageMagick.ResourceLimits.Memory = (ulong)options.LimitMB * (ulong)Math.Pow(1024, 2); // 2=MB, 3=GB ...

                    if (options.OderByName ?? true)
                        GlobalOrderFunc = (si) => si.Name;
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
        static void TryProcessFile(string tag, string file, string outputFolder, fileProcess callback)
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

        private static void ProcessAllFiles(SharedOptions opt,
                Action<string> singleFileJob,
                Action<string,DirectoryInfo,string> nestedFileJob = null, Action<string,DirectoryInfo,bool> onDirecotry = null)
        {
            log.i(LoggerHelper.Stringify("Files", opt.Files.ToArray()));
            var files = opt.Files.Select((f)=>new FileInfo(f)).OrderBy(GlobalOrderFunc);
            foreach (var _input_fi in files)
            {
                if (_input_fi.Exists)
                {
                    singleFileJob(_input_fi.FullName);
                }
                else
                {
                    log.e("Can't find input file '" + _input_fi.FullName + "', skipping");
                }
            }

            log.i(LoggerHelper.Stringify("Folders", opt.Folders.ToArray()));
            var dirs = opt.Folders.Select((d) => new DirectoryInfo(d)).OrderBy(GlobalOrderFunc).Cast<DirectoryInfo>();
            foreach (var _input_di in dirs)
            {
                if (_input_di.Exists)
                {
                    foreach (DirectoryInfo _recDi in _input_di.GetDirectories("*", SearchOption.AllDirectories).OrderBy(GlobalOrderFunc))
                    {
                        onDirecotry?.Invoke(_input_di.FullName, _recDi, true); // start?
                        foreach (FileInfo fileInDir in _recDi.GetFiles().OrderBy(GlobalOrderFunc))
                        {
                            if (nestedFileJob != null)
                            {
                                nestedFileJob(fileInDir.FullName, fileInDir.Directory, _input_di.FullName);
                            }
                            else
                            {
                                singleFileJob(fileInDir.FullName);
                            }
                        }
                        onDirecotry?.Invoke(_input_di.FullName, _recDi, false); // start?
                    }
                }
                else
                {
                    log.e("Can't find input dir '" + _input_di.FullName + "', skipping");
                }
            }
        }

        static void Color(ColorOptions opt)
        {
            Action<string, string> convertFile = new Action<string, string>((string input_file, string outputFolder) =>
             {
                 TryProcessFile("color-convert", input_file, outputFolder, (file, outfile, log) =>
                 {
                     using (var fileResult = RemoveBlack.FromFile(file, opt.fuzz, log, opt.UsePencilTile))
                     {
                         log.i("Writing to file: " + outfile);
                         fileResult.Write(outfile);
                     }
                 });
             });

            Action<string> _file_job = new Action<string>((string input_file) =>
            {
                convertFile(input_file, opt.OutputFolder);
            });

            Action<string,DirectoryInfo,string> _file_nested_job = new Action<string, DirectoryInfo, string>(
            (string input_file, DirectoryInfo currentInputDir, string inputFolderParam) =>
            {
                convertFile(input_file, currentInputDir.FullName.Replace(inputFolderParam, opt.OutputFolder));
            });


            ProcessAllFiles(opt, _file_job, nestedFileJob: _file_nested_job);
        }

        static void Duplex(DuplexOptions opt)
        {
            Duplex duplexBuilder = new Duplex(new LoggerHelper("duplex"),TemplatesTools.getA4(150,!(opt.Landscape ?? true)),opt.Columns);

            Action<string> _file_job = new Action<string>((string input_file) =>
            {
                TryProcessFile("duplex-job-single", input_file, opt.OutputFolder, (file, output_file,log) =>
                 {
                     duplexBuilder.saveCahpterFragmentsInto_PNG_LTR(
                         SplitFile(opt, input_file, file, output_file, log, duplexBuilder),
                         "",
                         opt.OutputFolder);
                 });
            });


            List<PageFragmnet> dirFragments = new List<PageFragmnet>();
            Action<string, DirectoryInfo,string> _file_nested_job = 
                new Action<string, DirectoryInfo,string>((string input_file, DirectoryInfo dir, string inputDir) =>
            {
                TryProcessFile("duplex-job-nested", input_file, opt.OutputFolder, (file, output_file, log) =>
                {
                    dirFragments.AddRange(
                        SplitFile(opt, input_file, file, output_file, log,duplexBuilder));
                });
            });

            Action<string,DirectoryInfo, bool> onDir = new Action<string,DirectoryInfo, bool>(
                (string inputFolder,DirectoryInfo currentInputDir, bool started) =>
             {
                 if(!started)
                 {
                     DirectoryInfo currentOutputDir = new DirectoryInfo(currentInputDir.FullName.Replace(inputFolder, opt.OutputFolder));
                     if (!currentOutputDir.Exists)
                         currentOutputDir.Create();
                     log.i("Saving fragments after folder to " + currentOutputDir.FullName);
                     duplexBuilder.saveCahpterFragmentsInto_PNG_LTR(dirFragments, "",currentOutputDir.FullName);
                 }
                 else
                 {
                     dirFragments = new List<PageFragmnet>();
                 }
             });

            ProcessAllFiles(opt, _file_job,nestedFileJob: _file_nested_job,onDirecotry: onDir);

        }

        private static List<PageFragmnet> SplitFile(
            DuplexOptions opt, string input_file, string file, string output_file, LoggerHelper log, Duplex d)
        {
            log.i(string.Format("Splitting strip: '" + file + "'"));
            var toon = new WebtoonPage()
            {
                filpath = input_file
            };
            using (MagickImage img = new MagickImage(file))
            {
                toon.height = img.Height;
                toon.width = img.Width;
            }
            return d.splitPageLandscape(toon);         
        }
    }
}
