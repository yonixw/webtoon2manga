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
    public class DrawMockTests
    {
        [TestMethod()]
        public void drawTest()
        {
            DrawMock g = new DrawMock();
            g.setSize(0, new System.Drawing.Rectangle(1,1, 10, 10));

            Assert.IsFalse(g.isExpanded(0));
            g.draw(0, new System.Drawing.Rectangle(2, 3, 5, 6));
            Assert.IsFalse(g.isExpanded(0));

            g.draw(0, new System.Drawing.Rectangle(5, 7, 11, 11));
            g.draw(0, new System.Drawing.Rectangle(0, 7, 7, 11));

            Assert.AreEqual(0, g.getCol(0).X);
            Assert.AreEqual(1, g.getCol(0).Y);
            Assert.AreEqual(16, g.getCol(0).Width);
            Assert.AreEqual(17, g.getCol(0).Height);

            Assert.IsTrue(g.isExpanded(0));
        }
    }
}