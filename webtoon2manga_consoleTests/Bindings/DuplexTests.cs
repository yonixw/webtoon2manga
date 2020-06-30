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
            List<PageFragmnet> listToPrint = new Duplex(
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
                List<PageFragmnet> listToPrint = new Duplex(
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

            List<PageFragmnet> listToPrint = new Duplex(new Tools.LoggerHelper("test"), PageA4,
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
                    TotalHeight += listToPrint[i].Transform.Height;
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

            List<PageFragmnet> listToPrint = new Duplex(new Tools.LoggerHelper("test"), PageA4,
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
                    TotalHeight += listToPrint[i].Transform.Height;
                }

                float expected_new_height = stripH;
                Assert.AreEqual(expected_new_height, TotalHeight);
            }
        }

        [TestMethod()]
        public void CombiningTwoSplittedStripsTogether()
        {
           

            Size A4 = Bindings.TemplatesTools.getA4(96, false);
            Duplex duplexBuilder = new Duplex(
                    new Tools.LoggerHelper("test"),
                    A4,
                    3,
                    padPercent: 0f
            );

            int colW = A4.Width / 3;
            int colH = A4.Height;

            WebtoonPage page = new WebtoonPage()
            {
                filpath = "",
                width = colW,
                height = (int)(colH * 1.2f),
            };
            WebtoonPage page2 = new WebtoonPage()
            {
                filpath = "",
                width = colW,
                height = (int)(colH * 1.2f),
            };

            List<PageFragmnet> fragmantsToRead = new List<PageFragmnet>();
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page));
            fragmantsToRead.AddRange(duplexBuilder.splitPageLandscape(page2));

            int usedColumns = duplexBuilder.saveCahpterFragmentsInto_PNG_LTR(
                fragmantsToRead,
                "",
                "",
                dryRun: true
                );

            Assert.AreEqual(3, usedColumns);
        }
    }
}