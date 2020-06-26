using Microsoft.VisualStudio.TestTools.UnitTesting;
using webtoon2manga_console.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Bindings.Tests
{
    [TestClass()]
    public class DuplexTests
    {
        public static void printFrags(IEnumerable<PageFragmnet> list)
        {
            int i = 0;
            foreach(var frag in list)
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
            List<PageFragmnet> listToPrint = Duplex.splitPageLandscape(
                    page,
                    Bindings.TemplatesTools.getA4(96, false),
                    3,
                    padPercent: 2.3f
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
            int[] dpps = new int[] {96,150,300,600 };
            foreach (int dpp in dpps)
            {
                List<PageFragmnet> listToPrint = Duplex.splitPageLandscape(
                       page,
                       Bindings.TemplatesTools.getA4(dpp, false),
                       3,
                       padPercent: 2.3f
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

            float padds = 0.023f;

            List<PageFragmnet> listToPrint = Duplex.splitPageLandscape(
                    page,
                    Bindings.TemplatesTools.getA4(96, false),
                    3,
                    padPercent: padds * 100,
                    repeatColPercent: padds * 100
            );

            if (listToPrint.Count > 1)
            {
                printFrags(listToPrint);

                int TotalHeight = 0;
                TotalHeight += listToPrint[1].Transform.Y - listToPrint[0].Transform.Y;
                for (int i = 1; i < listToPrint.Count - 1; i++)
                {
                    TotalHeight += listToPrint[i+1].Transform.Y - listToPrint[i].Transform.Y;
                }
                TotalHeight += listToPrint[listToPrint.Count - 1].Transform.Height;

                float expected_new_height = page.height + page.height * padds * (listToPrint.Count - 1);
                Console.WriteLine(string.Format("TotalH: {0}, OriginalH: {1}, Estimated: {2}",
                        TotalHeight, page.height, expected_new_height
                    ));

                Assert.IsTrue(TotalHeight >= expected_new_height);
            }
        }
    }
}