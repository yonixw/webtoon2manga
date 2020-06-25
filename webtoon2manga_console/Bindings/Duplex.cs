using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Bindings
{
    public class PageFragmnet
    {
        public Rectangle Source;

        public PageFragmnet(int x,int y,int w,int h)
        {
            Source = new Rectangle(x, y, w, h);    
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
        public static List<PageFragmnet> splitPage(
            WebtoonPage page, Size pageSize, int column, int spacing)
        {
            return new List<PageFragmnet>();
        }
    }
}
