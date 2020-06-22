using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console
{
    class ImageFileInfo
    {
        public FileInfo file;
        public Size ImageSize;
        public bool error = false;

        static ImageFileInfo getFileInfo(FileInfo file)
        {
            return null;
        }
    }
   

    class ImagePart
    {
        public ImageFileInfo ImageSource;
        public Size PartSize;
        public Point Pos;
    }

}
