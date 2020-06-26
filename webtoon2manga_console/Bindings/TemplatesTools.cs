using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Bindings
{
    public class TemplatesTools
    {
        static Size A4_300PPI = new Size(2480, 3508);
        public static Size getA4(int PPI, bool Portratit)
        {
            //https://www.papersizes.org/a-sizes-in-pixels.htm
            // A3 is bigger x2, A5 is smaller x2
            if (Portratit)
                return new Size((int)((A4_300PPI.Width / 300.0f) * PPI), (int)((A4_300PPI.Height / 300.0f) * PPI));
            else
                return new Size((int)((A4_300PPI.Height / 300.0f) * PPI), (int)((A4_300PPI.Width / 300.0f) * PPI));
        }
    }
}
