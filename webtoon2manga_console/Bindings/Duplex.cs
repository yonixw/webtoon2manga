using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using webtoon2manga_console.Tools;

namespace webtoon2manga_console.Bindings
{
    public class PageFragmnet
    {
        public Rectangle Transform;
        public WebtoonPage pageSource;

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
        public Size pageSize;
        public int columnCount;
        public float repeatColumnPercent = 2.3f;
        public float padPercent = 2.3f;
        public LoggerHelper log;

        public Duplex(LoggerHelper log, Size pageSize,
            int column, float repeatColPercent = 2.3f, float padPercent = 2.3f)
        {
            this.pageSize = pageSize;
            this.columnCount = column;
            this.repeatColumnPercent = repeatColPercent;
            this.padPercent = padPercent;
            this.log = log;
        }


        public List<PageFragmnet> splitPageLandscape(
            WebtoonPage toon )
        {
            List<PageFragmnet> result = new List<PageFragmnet>();

            int toonW = toon.width;
            int toonH = toon.height;

            int pageW = pageSize.Width;
            int pageH = pageSize.Height;

            //=================================

            int absolutePad = (int)(pageSize.Width * padPercent * 0.01);
            int printableWidth = pageSize.Width - absolutePad * (columnCount + 1);
            int finalColW = printableWidth / columnCount;
            int finalColH = pageH - 2 * absolutePad;
            int repeatColPercentAbsolute = (int)(finalColH * repeatColumnPercent * 0.01);

            float toonFactor = finalColW * 1f / toonW; // Match Width
            int finalToonH = (int)Math.Ceiling(toonH * toonFactor);

            float fragsBeforeRepeatSplit = finalToonH * 1f / finalColH;
            int repeatAddedHeight = repeatColPercentAbsolute 
                    *  ((int)Math.Ceiling(fragsBeforeRepeatSplit) - 1);

            int finalFrags = (int)Math.Ceiling((finalToonH + repeatAddedHeight) * 1f / finalColH);
            //==================================

            
            int lastY = 0;
            int reapeatToonScale = (int)((toonH / fragsBeforeRepeatSplit) * 0.01 * repeatColumnPercent);
            SizeF fragSize = new SizeF(toonW, toonH / fragsBeforeRepeatSplit);

            for(int i=0; i<finalFrags;i++)
            {
                // First page without repeating from prev split
                if (i > 0)
                    lastY -= reapeatToonScale;

                PointF startPoint = new PointF(0, lastY);
                SizeF actualFragSize = fragSize;
                if (fragSize.Height > toonH- lastY)
                {
                    actualFragSize = new SizeF(fragSize.Width, toonH - lastY);
                }

                result.Add(
                    new PageFragmnet(startPoint, actualFragSize) {
                        pageSource = toon
                    });

                lastY += (int)(toonH / fragsBeforeRepeatSplit);
            }

            return result;
        }

        static Pen borderPen = new Pen(Color.Black, 4);

        public int saveCahpterFragmentsInto_PNG_LTR(List<PageFragmnet> allFragments, string prefix, string outputFolder, bool dryRun = false)
        {
            int faceNumber = 0;

            int absolutePad = (int)(pageSize.Width * padPercent * 0.01);
            int printableWidth = pageSize.Width - absolutePad * (columnCount + 1);
            int finalColW = printableWidth / columnCount;
            int finalColH = pageSize.Height - 2 * absolutePad;

            int fragmentIndex = 0;
            int writeColCount = 0;

            while (fragmentIndex < allFragments.Count)
            {
                // New Face
                log.i("Building duplex Face No." + faceNumber);
                using (Bitmap faceBit = dryRun ? new Bitmap(1,1) : new Bitmap(pageSize.Width, pageSize.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(faceBit))
                    {
                        if (!dryRun)
                            g.FillRectangle(Brushes.White, new Rectangle(0, 0, pageSize.Width, pageSize.Height));
                        for (int i = 0; i < columnCount; i++)
                        {
                            int startY = absolutePad;
                            int endY = absolutePad + finalColH;
                            int startX = absolutePad + (finalColW + absolutePad) * (i);
                            int endX = startX + finalColW;

                            Rectangle area = new Rectangle(startX, startY, (endX - startX), (endY - startY));
                            if (!dryRun)
                                g.DrawRectangle(borderPen, area);
                            if (fragmentIndex < allFragments.Count)
                            {
                                log.i("Reading fragment " + fragmentIndex + " from " + Path.GetFileName(allFragments[fragmentIndex].pageSource.filpath));
                                if(!dryRun)
                                {
                                    using (Image fragSource = Bitmap.FromFile(allFragments[fragmentIndex].pageSource.filpath))
                                    {
                                        g.DrawImage(fragSource, area, allFragments[fragmentIndex].Transform, GraphicsUnit.Pixel);
                                    }
                                }
                                fragmentIndex++;
                                writeColCount++;
                            }
                        }
                    }
                    if (!dryRun)
                        faceBit.Save(
                            Path.Combine(outputFolder, string.Format("{0}page{1}.png", prefix, faceNumber))
                    );
                    faceNumber++;
                }
            }

            return writeColCount;
        }

    }
}
