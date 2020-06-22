using ImageMagick;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using webtoon2manga_console.Graphics;
using webtoon2manga_console.Tools;

namespace webtoon2manga_console
{
    class Program
    {
        static LoggerHelper log = new LoggerHelper("MAIN");

       // TODO: GrayScale + Lightness (blue)

        static void Main(string[] args)
        {
            // Less memory=>Slow
            ImageMagick.ResourceLimits.Memory = 750 * (ulong)Math.Pow(1024,2); // 2=MB, 3=GB ...

            log.i(LoggerHelper.Stringify("Args", args));

            string sourceFolder = @"C:\Users\Yoni\Desktop\2020\webtoon2manga_console\Samples";
            if (args.Length > 0)
                sourceFolder = args[0];

            string f1 = @"C:\Users\Yoni\Desktop\2020\webtoon2manga_console\Samples\tryouts\005.webp.png";

            log.i("Started " + f1);
            var result = RemoveBlack.FromFile(f1, true);
            result.Write(f1 + "_result.png");
            result.Dispose();




            // ====================== EXIT =======================
            Console.WriteLine("Enter to exit...");
            //Console.ReadLine();
        }
    }
}
