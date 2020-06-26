using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Bindings
{
    public class PageFragmnet
    {
        public Rectangle Transform;

        public PageFragmnet(int x,int y,int w,int h)
        {
            Transform = new Rectangle(x, y, w, h);    
        }

        public PageFragmnet(PointF pos, SizeF size)
        {
            Transform = new Rectangle((int)pos.X, (int)pos.Y, (int)size.Width, (int)size.Height);
        }

        public override string ToString()
        {
            return string.Format("({0},{1})+[{2},{3}]=({4},{5})",
                Transform.X,Transform.Y,
                0,Transform.Height,
                Transform.X,Transform.Y + Transform.Height
                );
        }
    }

    public class WebtoonPage
    {
        public string filpath;
        public int width;
        public int height;
    }

    public class Duplex
    {
        public static List<PageFragmnet> splitPageLandscape(
            WebtoonPage toon, Size pageSize,
            int column, float repeatColPercent = 2.3f, float padPercent = 2.3f)
        {
            List<PageFragmnet> result = new List<PageFragmnet>();

            int toonW = toon.width;
            int toonH = toon.height;

            int pageW = pageSize.Width;
            int pageH = pageSize.Height;

            //=================================

            int absolutePad = (int)(pageSize.Width * padPercent * 0.01);
            int printableWidth = pageSize.Width - absolutePad * (column + 1);
            int finalColW = printableWidth / column;
            int finalColH = pageH - 2 * absolutePad;
            int repeatColPercentAbsolute = (int)(finalColH * repeatColPercent * 0.01);

            float toonFactor = finalColW * 1f / toonW; // Match Width
            int finalToonH = (int)Math.Ceiling(toonH * toonFactor);

            float fragsBeforeRepeatSplit = finalToonH * 1f / finalColH;
            int repeatAddedHeight = repeatColPercentAbsolute 
                    *  ((int)Math.Ceiling(fragsBeforeRepeatSplit) - 1);

            int finalFrags = (int)Math.Ceiling((finalToonH + repeatAddedHeight) * 1f / finalColH);
            //==================================

            
            int lastY = 0;
            int reapeatToonScale = (int)((toonH / fragsBeforeRepeatSplit) * 0.01 * repeatColPercent);
            SizeF fragSize = new SizeF(toonW, toonH / fragsBeforeRepeatSplit);

            for(int i=0; i<finalFrags;i++)
            {
                // First page without repeating from prev split
                if (i > 0)
                    lastY -= reapeatToonScale;

                PointF startPoint = new PointF(0, lastY);
                result.Add(new PageFragmnet(startPoint, fragSize));

                lastY += (int)(toonH / fragsBeforeRepeatSplit);
            }

            return result;
        }
    }
}
