using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace webtoon2manga_console.Graphics
{
    //C# code https://www.imagemagick.org/discourse-server/viewtopic.php?t=33531

    /*
    SET f=31.jpg

    REM Get the black 
    convert %f% -channel rgba  -fuzz 1%% -fill none +opaque black bg_mask.png
    convert bg_mask.png -morphology Erode Disk  -morphology Dilate Disk   bg_finer.png
    convert bg_finer.png -transparent white -alpha extract  bg_finer_not.png

    REM Get none black (fuzz as blur)
    convert %f% -colorspace HSL -channel lightness -separate +channel 
        ( bg_finer_not.png -transparent white ) -compose copy-opacity -composite content.png

    REM Patterns
    REM https://imagemagick.org/script/formats.php#builtin-patterns
    REM CROSSHATCH45
    convert bg_finer.png -transparent white -alpha extract -negate 
        ( +clone ( -tile pattern:GRAY95 -resize 500%% ) -draw "color 0,0 reset" +clone +swap -compose color_dodge -composite )  
        bg_mask_pattern.png
    convert -composite bg_mask_pattern-1.png content.png final.png
    */

    class RemoveBlack
    {
        public static MagickImage FromFile(FileInfo file, int fuzz, bool usePencil = false)
        {
            return FromFile(file.FullName,fuzz, usePencil);
        }

        public static MagickImage FromFile(string file, int fuzz, bool usePencil = false)
        {
            MagickImage source = new MagickImage(file);
            MagickImage black_zones = extarctZones1(source, fuzz: fuzz);
            MagickImage finer_black_zones = removeSmallAreas2(black_zones);
            black_zones.Dispose();

            MagickImage finer_black_zones_not_AsMask = getAlphaMask3(finer_black_zones, "white");
            //
            using (MagickImage contentGrayscale = getContentLightnessMasked4(source, finer_black_zones_not_AsMask))
            {
                finer_black_zones_not_AsMask.Dispose();
                source.Dispose();
                using (MagickImage blackAsPattern = getPatternFillMasked5(finer_black_zones, usePencil: usePencil))
                {
                    finer_black_zones.Dispose();
                    MagickImage result = getResult6(blackAsPattern, contentGrayscale);

                    return result;
                }
            }
        }

        static MagickImage extarctZones1(MagickImage origin, int fuzz = 1, string color = "black")
        {
            //convert %f% -channel rgba  -fuzz 1%% -fill none +opaque black bg_mask.png
            var clone = (MagickImage)origin.Clone();
            clone.Alpha(AlphaOption.Set);
            clone.ColorFuzz = new Percentage(fuzz);
            clone.InverseOpaque(new MagickColor(color), MagickColors.Transparent);
            return clone;
        }

        static MagickImage removeSmallAreas2(MagickImage origin)
        {
            //convert bg_mask.png - morphology Erode Disk  -morphology Dilate Disk   bg_finer.png
            var clone = (MagickImage)origin.Clone();
            clone.Morphology(MorphologyMethod.Erode, Kernel.Disk);
            clone.Morphology(MorphologyMethod.Dilate, Kernel.Disk);
            return clone;
        }

        static MagickImage getAlphaMask3(MagickImage origin, string color = "white")
        {
            // convert bg_finer.png -transparent white -alpha extract  bg_finer_not.png
            var clone = (MagickImage)origin.Clone();
            clone.Transparent(new MagickColor(color));
            clone.Alpha(AlphaOption.Extract);
            return clone;
        }

        static MagickImage getContentLightnessMasked4(MagickImage origin, MagickImage mask)
        {
            //convert % f % -colorspace HSL - channel lightness - separate + channel
            //    (bg_finer_not.png - transparent white) - compose copy - opacity - composite content.png
            mask.Negate();
            var clone = (MagickImage)origin.Clone();
            clone.ColorSpace = ColorSpace.HSL;
            var lightness = (MagickImage)clone.Separate().Last();
            lightness.Composite(mask, 0, 0, CompositeOperator.CopyAlpha);
            //mask.Composite(lightness, 0, 0, CompositeOperator.CopyAlpha);
            return lightness;
        }

        static MagickImage pencilImage(int width=256, int height=256)
        {
            //convert -size 256x256 xc:  +noise Random  -virtual-pixel tile -motion-blur 0x20+135 -charcoal 1   pencil_tile.gif
            MagickImage result = new MagickImage(new MagickColor("white"), width, height);
            result.AddNoise(NoiseType.Random);
            result.MotionBlur(0, 20, 135);
            result.Charcoal(1, 1);
            result.ColorSpace = ColorSpace.Gray;
            result.Threshold(new Percentage(50));
            return result;
        }

        public static MagickImage PencilTile = pencilImage();

        static MagickImage getPatternFillMasked5(MagickImage origin,
            string color = "white", float tileFactor = 1.25f, bool usePencil =false)
        {
            /*
             * convert 
             *  bg_finer.png 
             *      -transparent white -alpha extract -negate 
                ( 
                    +clone ( -tile pattern:GRAY95 -resize 500%% ) 
                    -draw "color 0,0 reset"
                    +clone +swap -compose color_dodge -composite 
              )  
                bg_mask_pattern.png
            */
            var clone = (MagickImage)origin.Clone();
            clone.Transparent(new MagickColor(color));
            clone.Alpha(AlphaOption.Extract);
            clone.Negate();

            MagickImage tile = null;
            if (!usePencil)
            {
                tile = new MagickImage("PATTERN:GRAY95", // https://imagemagick.org/script/formats.php#builtin-patterns
                    (int)(clone.Width / tileFactor), (int)(clone.Height / tileFactor));
                tile.Resize(new Percentage(tileFactor * 100));
            }
            else
            {
                tile = pencilImage((int)(clone.Width / tileFactor), (int)(clone.Height / tileFactor));
                tile.Resize(new Percentage(tileFactor * 100));
            }

            var tiled_clone = (MagickImage)clone.Clone();
            tiled_clone.Composite(tile, CompositeOperator.ColorDodge); // add in away that interacts with all black

            tile.Dispose();

            return tiled_clone;
        }

        static MagickImage getResult6(MagickImage patternedBackground, MagickImage content)
        {
            /*
             * convert -composite bg_mask_pattern-1.png content.png final.png
            */
            var clone = content.Clone();
            clone.Composite(patternedBackground, CompositeOperator.Darken);
            return (MagickImage)clone;
        }
    }
}
