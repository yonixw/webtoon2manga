﻿using System;
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

   
    public class DrawMock 
    {
        Dictionary<int, Rectangle> colReigions = new Dictionary<int, Rectangle>();
        Dictionary<int, bool> colExapanded = new Dictionary<int, bool>();

        public void draw(int id, Rectangle target)
        {
            Rectangle r = colReigions[id];
            if (target.X < r.X)
            {
                int maxX = r.X + r.Width;
                r.X = target.X;
                r.Width = maxX - r.X;
                colExapanded[id] = true;
            }
            if (target.Y < r.Y)
            {
                int maxY = r.Y + r.Height;
                r.Y = target.Y;
                r.Height = maxY - r.Y;
                colExapanded[id] = true;
            }
            if (target.X + target.Width > r.X + r.Width)
            {
                r.Width += (target.X + target.Width) - (r.X + r.Width);
                colExapanded[id] = true;
            }
            if (target.Y + target.Height > r.Y + r.Height)
            {
                r.Height += (target.Y + target.Height) - (r.Y + r.Height);
                colExapanded[id] = true;
            }
            colReigions[id] = r;
        }

        public void setSize(int id, Rectangle target)
        {
            colReigions.Add(id, target);
            colExapanded.Add(id, false);
        }

        public Rectangle getCol(int id)
        {
            return colReigions[id];
        }

        public bool isExpanded(int id)
        {
            return colExapanded[id];
        }
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

        public int saveCahpterFragmentsInto_PNG_LTR(
            List<PageFragmnet> allFragments, string prefix, string outputFolder, DrawMock mock = null)
        {
            bool dryRun = (mock != null);
            int faceNumber = 0;

            int absolutePad = (int)(pageSize.Width * padPercent * 0.01);
            int printableWidth = pageSize.Width - absolutePad * (columnCount + 1);
            int finalSingleColW = printableWidth / columnCount;
            int finalSingalColH = pageSize.Height - 2 * absolutePad;

            int fragmentIndex = 0;
            int writeColCount = 0;

            // Height across aspects
            int currentColOffsetY = 0;
            int currentFragOffsetY = 0;

            while (fragmentIndex < allFragments.Count)
            {
                // New Face
                log.i("Building duplex Face No." + faceNumber);
                using (Bitmap faceBit = dryRun ? new Bitmap(1,1) : new Bitmap(pageSize.Width, pageSize.Height))
                {
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(faceBit))
                    {
                        // White background
                        if (!dryRun)
                            g.FillRectangle(Brushes.White, new Rectangle(0, 0, pageSize.Width, pageSize.Height));

                        for (int i = 0; i < columnCount; i++)
                        {
                            int startY = absolutePad;
                            int endY = absolutePad + finalSingalColH;
                            int startX = absolutePad + (finalSingleColW + absolutePad) * (i);
                            int endX = startX + finalSingleColW;

                            Rectangle area = new Rectangle(startX, startY, (endX - startX), (endY - startY));

                            if (dryRun)
                                mock?.setSize(writeColCount, area);

                            bool drawnToCol = false;
                            currentColOffsetY = 0;
                            while(currentColOffsetY < area.Height && fragmentIndex < allFragments.Count)
                            {
                                if (!dryRun)
                                    g.DrawRectangle(borderPen, area);

                                if (fragmentIndex < allFragments.Count)
                                {
                                    drawnToCol = true;
                                    var currFrag = allFragments[fragmentIndex];

                                    float colHPercentLeft = 1f - currentColOffsetY *1f / area.Height;
                                    float fullFragHeightAsColPercent = 
                                        ((area.Width * 1f / currFrag.Transform.Width) * currFrag.Transform.Height) / area.Height;
                                    float drawnFragPercent =
                                        currentFragOffsetY  * 1f / currFrag.Transform.Height;
                                    
                                    Rectangle newTarget = new Rectangle(area.Location, area.Size);
                                    
                                    log.i("Using fragment " + fragmentIndex + " from " + Path.GetFileName(currFrag.pageSource.filpath));
                                    if ((fullFragHeightAsColPercent - drawnFragPercent) > colHPercentLeft)
                                    {
                                        //draw and leave some for next while
                                        float drawPercent = Math.Min(colHPercentLeft, fullFragHeightAsColPercent);
                                        Rectangle newSource = new Rectangle(currFrag.Transform.Location, currFrag.Transform.Size);
                                        newSource.Height =(int)(newSource.Height* drawPercent);
                                        newSource.Offset(0, currentFragOffsetY);

                                        newTarget.Height = (int)(fullFragHeightAsColPercent * drawPercent);
                                        newTarget.Offset(0, currentColOffsetY);

                                        // Draw part frag 
                                        if (!dryRun)
                                        {
                                            using (Image fragSource = Bitmap.FromFile(currFrag.pageSource.filpath))
                                            {
                                                g.DrawImage(fragSource, newTarget, newSource, GraphicsUnit.Pixel);
                                            }
                                        }
                                        else
                                        {
                                            mock?.draw(writeColCount, newTarget);
                                        }

                                        currentFragOffsetY += newSource.Height;
                                        currentColOffsetY = area.Height + 1;
                                    }
                                    else
                                    {
                                        newTarget.Height = (int)(fullFragHeightAsColPercent * (endY - startY));
                                        newTarget.Offset(0, currentColOffsetY);

                                        // Draw entire frag 
                                        if (!dryRun)
                                        {
                                            using (Image fragSource = Bitmap.FromFile(currFrag.pageSource.filpath))
                                            {
                                                g.DrawImage(fragSource, newTarget, currFrag.Transform, GraphicsUnit.Pixel);
                                            }
                                        }
                                        else
                                        {
                                            mock?.draw(writeColCount, newTarget);
                                        }

                                        currentColOffsetY += newTarget.Height;
                                        fragmentIndex++;
                                    }
                                }
                            }

                            if (drawnToCol)
                            {
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
