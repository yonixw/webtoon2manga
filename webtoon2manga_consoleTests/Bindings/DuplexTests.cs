using Microsoft.VisualStudio.TestTools.UnitTesting;
using webtoon2manga_console.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace webtoon2manga_console.Bindings.Tests
{
    [TestClass()]
    public class DuplexTests
    {
        public static void printFrags(IEnumerable<PageFragmnet> list)
        {
            int i = 0;
            foreach (var frag in list)
            {
                Console.WriteLine("#" + i + ", " + frag.ToString());
                i++;
            }
        }

        [TestMethod()]
        public void splitPageTest()
        {
            /*
             * 	Webtoon
                    W	800
                    H	12480
	
	            A4
	                 W  1123
	                 H  794
	
                    Col	3
                    Pad 	25
	
                    FinalWidth	341
                    FinalHeight	744
	
                    Webtoon	
                    Scale	2.346041056
                    W	341
                    H	5319.6
	
                    Frag	7.15
	
                    SplitRepeat	25
                    Total Cols	7.385215054

            */

            WebtoonPage page = new WebtoonPage()
            {
                filpath = "",
                width = 800,
                height = 12480,
            };
            List<PageFragmnet> listToPrint = new DuplexBuilder(
                new Tools.LoggerHelper("test"),
                Bindings.TemplatesTools.getA4(96, false),
                    3,
                    padPercent: 2.3f
            ).splitPageLandscape(
                    page
            );

            Assert.AreEqual(8, listToPrint.Count);
        }

        [TestMethod()]
        public void splitPageTest2()
        {
            /*
             * 	Webtoon
                    W	690
                    H	1885
	
	            A4
	                 W  1123
	                 H  794
	
                    Col	 3
                    Pad 	25
	
                    FinalWidth	341
                    FinalHeight	744
	
                    Webtoon	
                    Scale	2.53360
                    W	341
                    H	931
	
                    Frag	7.15
	
                    SplitRepeat	25
                    Total Cols	7.385215054

            */

            WebtoonPage page = new WebtoonPage()
            {
                filpath = "",
                width = 690,
                height = 1885,
            };
            List<PageFragmnet> listToPrint = new DuplexBuilder(
                new Tools.LoggerHelper("test"),
                Bindings.TemplatesTools.getA4(96, false),
                    3,
                    padPercent: 2.3f
            ).splitPageLandscape(
                    page
            );

            Assert.AreEqual(2, listToPrint.Count);
        }


        


        [TestMethod()]
        public void splitPageAspectConsistency()
        {
            WebtoonPage page = new WebtoonPage()
            {
                filpath = "",
                width = 800,
                height = 12480,
            };
            int[] dpps = new int[] { 96, 150, 300, 600 };
            foreach (int dpp in dpps)
            {
                List<PageFragmnet> listToPrint = new DuplexBuilder(
                          new Tools.LoggerHelper("test"),
                          Bindings.TemplatesTools.getA4(dpp, false),
                          3,
                          padPercent: 2.3f
                  ).splitPageLandscape(
                          page
                  );

                Assert.AreEqual(8, listToPrint.Count);
            }
        }


        [TestMethod()]
        public void splitFragCompletness()
        {
            WebtoonPage page = new WebtoonPage()
            {
                filpath = "",
                width = 800,
                height = 12480,
            };

            var PageA4 = Bindings.TemplatesTools.getA4(96, false);

            List<PageFragmnet> listToPrint = new DuplexBuilder(new Tools.LoggerHelper("test"), PageA4,
                    3,
                    padPercent: 2.3f,
                    repeatColPercent: 2.3f
            ).splitPageLandscape(
                    page
            );

            Assert.IsTrue(listToPrint.Count > 1);
            if (listToPrint.Count > 1)
            {
                printFrags(listToPrint);

                int TotalHeight = 0;
                for (int i = 0; i < listToPrint.Count; i++)
                {
                    TotalHeight += listToPrint[i].SourceTransform.Height;
                }

                Assert.AreEqual(12760,TotalHeight);
            }
        }

        [TestMethod()]
        public void splitFragCompletness2()
        {
            var PageA4 = Bindings.TemplatesTools.getA4(96, false);

            int stripW = PageA4.Width / 3; // 1/3 so height is exact
            int stripH = PageA4.Height * 2 + PageA4.Height / 2; //2.5

            WebtoonPage toonStrip = new WebtoonPage()
            {
                filpath = "",
                width = stripW ,
                height = stripH,
            };

            List<PageFragmnet> listToPrint = new DuplexBuilder(new Tools.LoggerHelper("test"), PageA4,
                    3,
                    padPercent: 0,
                    repeatColPercent: 0
            ).splitPageLandscape(
                    toonStrip
            );

            Assert.IsTrue(listToPrint.Count > 1);
            if (listToPrint.Count > 1)
            {
                printFrags(listToPrint);

                int TotalHeight = 0;
                for (int i = 0; i < listToPrint.Count; i++)
                {
                    TotalHeight += listToPrint[i].SourceTransform.Height;
                }

                float expected_new_height = stripH;
                Assert.AreEqual(expected_new_height, TotalHeight);
            }
        }

        

        [TestMethod()]
        public void CombiningTwoSplittedStripsTogether()
        {
            Size A4 = Bindings.TemplatesTools.getA4(96, false);
            DuplexBuilder duplexBuilder = new DuplexBuilder(
                    new Tools.LoggerHelper("test"),
                    A4,
                    3,
                    padPercent: 0f
            );

            int colW = A4.Width / 3;
            int colH = A4.Height;

            WebtoonPage page = new WebtoonPage()
            {
                filpath = "p1.png",
                width = colW,
                height = (int)(colH * 1.2f),
            };
            WebtoonPage page2 = new WebtoonPage()
            {
                filpath = "p2.png",
                width = colW,
                height = (int)(colH * 1.2f),
            };

            DrawMock mock = new DrawMock();
            List<PageFragmnet> fragmantsToRead = new List<PageFragmnet>();
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page));
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page2));

            printFrags(fragmantsToRead);

            var outputPages = duplexBuilder.saveCahpterFragmentsInto_PNG_LTR(
                fragmantsToRead,
                "",
                "",
                mock: mock
                );

            var usedColumns = outputPages.Sum((p) => p.ColCount());

            CheckDrawInsideBounds(mock, usedColumns,3 );
        }

        private static void CheckDrawInsideBounds(DrawMock mock, int usedColumns, int expectedCol)
        {
            Assert.AreEqual(expectedCol, usedColumns);
            for (int i = 0; i < usedColumns; i++)
            {
                Assert.IsFalse(mock.isExpanded(i));
            }
        }

        [TestMethod()]
        public void CombiningTwoSplittedStripsTogether2()
        {


            Size A4 = Bindings.TemplatesTools.getA4(96, false);
            DuplexBuilder duplexBuilder = new DuplexBuilder(
                    new Tools.LoggerHelper("test"),
                    A4,
                    3,
                    padPercent: 0f
            );

            WebtoonPage page = new WebtoonPage()
            {
                filpath = "p1.png",
                width = 690,
                height = 212,
            };
            WebtoonPage page2 = new WebtoonPage()
            {
                filpath = "p2.png",
                width = 690,
                height = 1885,
            };

            DrawMock mock = new DrawMock();


            List<PageFragmnet> fragmantsToRead = new List<PageFragmnet>();
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page));
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page2));

            printFrags(fragmantsToRead);

            var outputPages = duplexBuilder.saveCahpterFragmentsInto_PNG_LTR(
               fragmantsToRead,
               "",
               "",
               mock: mock
               );

            var usedColumns = outputPages.Sum((p) => p.ColCount());
            var usedPrintOrders  = outputPages.Sum((p) => (p.GetCols.Sum((c) => c.getPrintSources.Count) ));
            Assert.AreEqual(4, usedPrintOrders);

            CheckDrawInsideBounds(mock, usedColumns, 2);
        }


        [TestMethod()]
        public void CombiningTwoSplittedStripsTogether_ConsistentY()
        {


            Size A4 = Bindings.TemplatesTools.getA4(96, false);
            DuplexBuilder duplexBuilder = new DuplexBuilder(
                    new Tools.LoggerHelper("test"),
                    A4,
                    3,
                    padPercent: 0f
            );

            WebtoonPage page = new WebtoonPage()
            {
                filpath = "p1.png",
                width = 690,
                height = 212,
            };
            WebtoonPage page2 = new WebtoonPage()
            {
                filpath = "p2.png",
                width = 690,
                height = 1885,
            };

            DrawMock mock = new DrawMock();


            List<PageFragmnet> fragmantsToRead = new List<PageFragmnet>();
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page));
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page2));

            printFrags(fragmantsToRead);

            var outputPages = duplexBuilder.saveCahpterFragmentsInto_PNG_LTR(
               fragmantsToRead,
               "",
               "",
               mock: mock
               );

            var usedColumns = outputPages.Sum((p) => p.ColCount());

            CheckDrawInsideBounds(mock, usedColumns, 2);
            VerifyConsistentOffset(outputPages);
        }

        private static void VerifyConsistentOffset(List<OutputPage> outputPages)
        {
            foreach (var p in outputPages)
            {
                foreach (var c in p.GetCols)
                {
                    int Y = 0;
                    foreach (var ps in c.getPrintSources)
                    {
                        Assert.AreEqual(Y, ps.PartialTarget.Y);
                        Y += ps.PartialTarget.Height;
                    }
                    Assert.IsTrue(Y <= c.getArea.Height);
                }
            }
        }
    }
}