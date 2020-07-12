using ImageMagick;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
        public Rectangle SourceTransform;
        public Size TargetSize;
        public WebtoonPage pageSource;

        public PageFragmnet(int x, int y, int w, int h, float Aspect)
        {
            SourceTransform = new Rectangle(x, y, w, h);
            setTargetAspect(Aspect);
        }

        public PageFragmnet(PointF pos, SizeF size, float Aspect)
        {
            SourceTransform = new Rectangle((int)pos.X, (int)pos.Y, (int)size.Width, (int)size.Height);
            setTargetAspect(Aspect);
        }

        public void setTargetAspect(float Aspect)
        {
            TargetSize = new Size(
                    (int)(SourceTransform.Width * Aspect), (int)(SourceTransform.Height * Aspect)
                    );
        }

        private int _SourceYOffset = 0;
        public int SourceYOffset { get { return _SourceYOffset; } }
        private int _TargetYOffset = 0;
        public int TargetYOffset { get { return _TargetYOffset; } }

        public void addYOffset(int offset, bool isSourceTransform)
        {
            if (isSourceTransform)
            {
                if (SourceYOffset + offset > SourceTransform.Height)
                    throw new Exception("Offset is out of bound for source!");
                _SourceYOffset += offset;
                _TargetYOffset += (int)((offset * 1f / SourceTransform.Height) * TargetSize.Height);
            }
            else
            {
                if (TargetYOffset + offset > TargetSize.Height)
                    throw new Exception("Offset is out of bound for target!");
                _TargetYOffset += offset;
                _SourceYOffset += (int)((offset * 1f / TargetSize.Height) * SourceTransform.Height);
            }
        }

        public bool isFull(int accuracy = 0)
        {
            return 
                (SourceYOffset + accuracy >= SourceTransform.Height) || 
                (TargetYOffset + accuracy >= TargetSize.Height);
        }


        public override string ToString()
        {
            return string.Format("({0},{1})+[{2},{3}]=({4},{5})",
                SourceTransform.X,SourceTransform.Y,
                0,SourceTransform.Height,
                SourceTransform.X,SourceTransform.Y + SourceTransform.Height
                );
        }
    }

    public class OutputPartialPrintOrder
    {
        public Rectangle PartialSource;
        public Rectangle PartialTarget;
        public string filename = "";

        public OutputPartialPrintOrder(string SourcePath)
        {
            filename = SourcePath;
        }
    }

    public class OutputCol
    {
        Point myOffset;
        Size myArea;
        public Rectangle getArea { get { return new Rectangle(myOffset, myArea);  } }
        int myYOffset = 0;
        List<OutputPartialPrintOrder> mySources = new List<OutputPartialPrintOrder>();

        public List<OutputPartialPrintOrder> getPrintSources { get { return mySources; } }

        public OutputCol(Size colArea, Point colOffset)
        {
            myOffset = colOffset;
            myArea = colArea;
            myYOffset = 0;
        }

        public void addFragment(PageFragmnet frag, bool firstInCol)
        {
            if (isFull())
                throw new Exception("Column already full!");

            int myPrintableY = myArea.Height - myYOffset;
            int fragPrintableY = Math.Min(myPrintableY, frag.TargetSize.Height - frag.TargetYOffset);

            int fragStartYSource = frag.SourceYOffset;
            int fragStartYTarget =  frag.TargetYOffset;

            frag.addYOffset(fragPrintableY, false);

            int fragEndYSource = frag.SourceYOffset;

            OutputPartialPrintOrder printOrder = new OutputPartialPrintOrder(frag.pageSource.filpath);
            printOrder.PartialSource = new Rectangle(
                    new Point(0,fragStartYSource+frag.SourceTransform.Y),
                    new Size(frag.SourceTransform.Width, fragEndYSource- fragStartYSource)
            );

            printOrder.PartialTarget = new Rectangle(
                    new Point(myOffset.X,myOffset.Y+myYOffset
                        +  (firstInCol? 0: fragStartYTarget)),
                    new Size(myArea.Width, (frag.TargetYOffset- fragStartYTarget))
            );
            mySources.Add(printOrder);

            //Console.WriteLine("Partial Source:" + printOrder.PartialSource.ToString());
            //Console.WriteLine("Partial Target:" + printOrder.PartialTarget.ToString());

            myYOffset += printOrder.PartialTarget.Height;
        }

        public bool isFull(int accuracy = 0)
        {
            return myYOffset + accuracy >= myArea.Height;
        }
    }

    public class OutputPage
    {
        List<OutputCol> myCol = new List<OutputCol>();
        int maxColCount = -1;
        Size colSize;
        int absolutePad = 0;
        int absoluteSpace = 0;

        public List<OutputCol> GetCols { get { return myCol; } }

        public OutputPage(int columnCount, Size pageSize, float padPercent, float spacePercent)
        {
            maxColCount = columnCount;

            absolutePad = (int)(pageSize.Width * padPercent * 0.01);
            absoluteSpace = (int)(pageSize.Width * spacePercent * 0.01);
            int printableWidth = pageSize.Width - absoluteSpace * (columnCount - 1) - 2 * absolutePad;
            int finalSingleColW = printableWidth / columnCount;
            int finalSingleColH = pageSize.Height - 2 * absolutePad;

            colSize = new Size(finalSingleColW, finalSingleColH);
        }

        public OutputCol NewCol()
        {
            Point offset = new Point();
            offset.Y = absolutePad;
            offset.X = absolutePad + myCol.Sum((c) =>  absoluteSpace + c.getArea.Width);

            OutputCol col = new OutputCol(colSize,offset);
            myCol.Add(col);
            // Console.WriteLine("New COL");
            return col;
        }

        public int ColCount() { return myCol.Count; }
    }

    public class WebtoonPage
    {
        public string filpath;
        public int width;
        public int height;
    }
    
    public class DuplexBuilder
    {
        public Size pageSize;
        public int columnCount;
        public float repeatColumnPercent = 2.3f;
        public float padPercent = 2.3f;
        public float spacePercent = 2.3f;
        public LoggerHelper log;

        public DuplexBuilder(LoggerHelper log, Size pageSize,
            int column, 
            float repeatColPercent = 2.3f, float padPercent = 2.3f, float spacePercent = 2.3f)
        {
            this.pageSize = pageSize;
            this.columnCount = column;
            this.repeatColumnPercent = repeatColPercent;
            this.padPercent = padPercent;
            this.spacePercent = spacePercent;
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
            int absoluteSpace = (int)(pageSize.Width * spacePercent * 0.01);
            int printableWidth = pageSize.Width - absoluteSpace * (columnCount -1 ) - 2*absolutePad;
            int finalColW = printableWidth / columnCount;
            int finalColH = pageH - 2 * absolutePad;

            float toonFactor = finalColW * 1f / toonW; // Match Width
            int finalToonH = (int)Math.Ceiling(toonH * toonFactor);

            float fragsBeforeRepeatSplit = finalToonH * 1f / finalColH;

            int finalFrags = (int)Math.Ceiling((finalToonH ) * 1f / finalColH);
            //==================================

            
            int lastY = 0;
            SizeF fragSize = new SizeF(toonW, toonH / fragsBeforeRepeatSplit);

            for(int i=0; i<finalFrags;i++)
            {
                PointF startPoint = new PointF(0, lastY);
                SizeF actualFragSize = fragSize;
                if (fragSize.Height > toonH- lastY)
                {
                    actualFragSize = new SizeF(fragSize.Width, toonH - lastY);
                }

                var frag = new PageFragmnet(startPoint, actualFragSize, toonFactor) {pageSource = toon};
                result.Add(frag);

                lastY += (int)(toonH / fragsBeforeRepeatSplit);
            }

            return result;
        }

        static Pen borderPen = new Pen(Color.Black, 4);

        public Rectangle substractYRectangle(Rectangle r, int Y)
        {
            return new Rectangle(r.X, r.Y - Y, r.Width, r.Height + Y);
        }

        public Rectangle shiftYRectangle(Rectangle r, int Y)
        {
            return new Rectangle(r.X, r.Y + Y, r.Width, r.Height );
        }

        public List<OutputPage> saveCahpterFragmentsInto_PNG_LTR(
              List<PageFragmnet> allFragments, string prefix, string outputFolder, DrawMock mock = null)
        {
            bool dryRun = (mock != null);

            Size virtualPage = new Size(pageSize.Width, (int)(pageSize.Height *(1- repeatColumnPercent * 0.01f)));
            int repeatHeight = pageSize.Height - virtualPage.Height ;
            Rectangle virtualRepeat = new Rectangle(0, 0, virtualPage.Width, repeatHeight);

            // Split to parts
            List<OutputPage> outputPages = new List<OutputPage>();
            int fragmentIndex = 0;
            while (fragmentIndex < allFragments.Count)
            {
                //Console.WriteLine("New Page");
                OutputPage face = new OutputPage(columnCount, virtualPage, padPercent, spacePercent);
                outputPages.Add(face);

                for (int c=0;c<columnCount && fragmentIndex < allFragments.Count; c++)
                {
                    OutputCol col = face.NewCol();
                    bool firstFrag = true;
                    while (!col.isFull() && fragmentIndex < allFragments.Count)
                    {
                        col.addFragment(allFragments[fragmentIndex], firstFrag);
                        firstFrag = false;
                        if (allFragments[fragmentIndex].isFull())
                            fragmentIndex++;
                    }
                }
            }

            if (dryRun)
            {
                var jsonString = JsonConvert.SerializeObject(
                   outputPages, Formatting.Indented,
                   new JsonConverter[] { new StringEnumConverter() });
                Console.WriteLine(jsonString);
            }

            // Width bigger just because we dont know col width yet.
            Bitmap repeatCol = new Bitmap(virtualPage.Width, repeatHeight);
            using (var rG = System.Drawing.Graphics.FromImage(repeatCol))
            {
                rG.FillRectangle(Brushes.Gray, virtualRepeat);

                // Print to file
                int colUniqueIndex = 0;
                int pageIndex = 0;
                bool hasRepeat = false;
                foreach (var page in outputPages)
                {
                    log.i("Creating new page..");
                    using (Bitmap faceBit = dryRun ? new Bitmap(1, 1) : new Bitmap(pageSize.Width, pageSize.Height))
                    {
                        using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(faceBit))
                        {
                            // White background
                            if (!dryRun)
                                g.FillRectangle(Brushes.White, new Rectangle(0, 0, pageSize.Width, pageSize.Height));

                            Rectangle lastPartialShiftedTarget = new Rectangle(0,0,0,0);
                            foreach (var col in page.GetCols)
                            {
                                log.i("Creating new cols..");

                                // Handle also the shifting:
                                Rectangle actualFullCol = shiftYRectangle(substractYRectangle(col.getArea, repeatHeight),repeatHeight); 

                                if (dryRun)
                                    mock?.setSize(colUniqueIndex, col.getArea);
                                else
                                    g.DrawRectangle(borderPen, actualFullCol); // Col border

                                if (hasRepeat)
                                {
                                    // Print repeat from last col
                                    g.DrawImage(
                                        repeatCol,
                                        new Rectangle(actualFullCol.Location,new Size(actualFullCol.Width,repeatHeight))
                                        , virtualRepeat, GraphicsUnit.Pixel);
                                }

                                foreach (var part in col.getPrintSources)
                                {
                                    if (dryRun)
                                        mock?.draw(colUniqueIndex, part.PartialTarget);
                                    else
                                    {
                                        log.i("Creating frag from '" + part.filename + "'");
                                        using (Image fragSource = Bitmap.FromFile(part.filename))
                                        {
                                            // Shifted by substracting Y 
                                            lastPartialShiftedTarget = shiftYRectangle(part.PartialTarget,repeatHeight);
                                            g.DrawImage(fragSource, lastPartialShiftedTarget , part.PartialSource, GraphicsUnit.Pixel);
                                            hasRepeat = true;
                                        }
                                    }
                                }

                                // Save last repeat:
                                Rectangle repeatTarget = lastPartialShiftedTarget;
                                repeatTarget.Y = lastPartialShiftedTarget.Y + lastPartialShiftedTarget.Height - repeatHeight;
                                repeatTarget.Height = repeatHeight;
                                rG.DrawImage(faceBit, virtualRepeat, repeatTarget , GraphicsUnit.Pixel);
                                colUniqueIndex++;
                            }
                        }

                        if (!dryRun)
                        {
                            string saveFile = Path.Combine(outputFolder, string.Format("{0}page{1:0000}.png", prefix, pageIndex));
                            log.i("Saving to '" + saveFile + "'");
                            faceBit.Save(saveFile);
                        }

                        pageIndex++;
                    }
                }

                repeatCol.Dispose();
                return outputPages;
            }
        }
    }
}
