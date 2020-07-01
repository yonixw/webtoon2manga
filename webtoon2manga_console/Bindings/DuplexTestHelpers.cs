using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Bindings
{
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




}
