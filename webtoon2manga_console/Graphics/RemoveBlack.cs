using ImageMagick;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using webtoon2manga_console.Tools;

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
        public static MagickImage FromFile(FileInfo file, int fuzz, LoggerHelper log, bool usePencil = false)
        {
            return FromFile(file.FullName,fuzz,log, usePencil);
        }

        public static MagickImage FromFile(string file, int fuzz,LoggerHelper log, bool usePencil = false)
        {

            MagickImage source = new MagickImage(file);
            //source.Trim(); // Can't "copy" trim since we dont know new origin\offsets...

            log.i("(1/9) Detecting black background");
            MagickImage black_zones = extarctZones1(source, fuzz: fuzz);

            log.i("(2/9) Detecting empty vertical spaces");
            Size LiquidResizeTarget = removeDoubleLines(black_zones,source);
            Console.WriteLine("{0}x{1}->Liq->{2}x{3}", source.Width, source.Height, LiquidResizeTarget.Width, LiquidResizeTarget.Height);

            MagickGeometry geom = new MagickGeometry(LiquidResizeTarget.Width, LiquidResizeTarget.Height);
            geom.IgnoreAspectRatio = true;
            log.i("(3/9) Remove vertical spaces");
            source.LiquidRescale(geom);

            black_zones.Dispose();
            // Recalculate blackZones
            log.i("(4/9) Detecting black background (again)");
            black_zones = extarctZones1(source, fuzz: fuzz);
            log.i("(5/9) Remove black background small areas");
            MagickImage finer_black_zones = removeSmallAreas2(black_zones);
            black_zones.Dispose();

            log.i("(6/9) Black background as mask");
            MagickImage finer_black_zones_not_AsMask = getAlphaMask3(finer_black_zones, "white");

            log.i("(7/9) Get grasyscale (but with HSL)");
            using (MagickImage contentGrayscale = getContentLightnessMasked4(source, finer_black_zones_not_AsMask))
            {
                finer_black_zones_not_AsMask.Dispose();
                source.Dispose();

                log.i("(8/9) Replace background with Pattern");
                using (MagickImage blackAsPattern =  getPatternFillMasked5(finer_black_zones, usePencil: usePencil))
                {
                    finer_black_zones.Dispose();
                    log.i("(9/9) Combine All");
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

        static MagickImage removeSmallAreas2(MagickImage origin, bool useThershold =false)
        {
            //convert bg_mask.png - morphology Erode Disk  -morphology Dilate Disk   bg_finer.png
            var clone = (MagickImage)origin.Clone();
            clone.Morphology(MorphologyMethod.Erode, Kernel.Disk);
            clone.Morphology(MorphologyMethod.Dilate, Kernel.Disk);
            if (useThershold)
                clone.Threshold(new Percentage(50));
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
            tiled_clone.Opaque(new MagickColor("black"), new MagickColor("#77"));

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


        static MagickColor GetColorBytes(byte[] pixel, int offset, int bytecount)
        {
            MagickColor result = new MagickColor();

            switch(bytecount)
            {
                case 1: // Grayscale
                    result = new MagickColor(pixel[offset + 0], pixel[offset + 0], pixel[offset + 0]);
                    break;
                case 3: // RGB
                    result = new MagickColor(pixel[offset + 0], pixel[offset + 1], pixel[offset + 2]);
                    break;
                case 4: // RGBA
                    result = new MagickColor(pixel[offset + 0], pixel[offset + 1], pixel[offset + 2], pixel[offset + 3]);
                    break;
            }

            return result;
        }

        static bool isPixelSame(MagickColor startColor,byte[] pixel, int byteCount,int offset)
        {
            return Math.Abs(startColor.R - pixel[0]) < 10;
        }

        static Size removeDoubleLines(MagickImage mask, MagickImage source)
        {
            //http://www.imagemagick.org/Usage/resize/#sample
            using (MagickImage greyscale = (MagickImage)source.Clone())
            {

                // clone.ColorSpace = ColorSpace.RGB;
                // clone.Alpha(AlphaOption.Set);
                //return new Size(clone.Width, clone.Height);

                greyscale.ColorSpace = ColorSpace.HSL;
                var lightness = (MagickImage)greyscale.Separate().Last();

                using (var pixels_mask = mask.GetPixelsUnsafe())
                {
                    using (var pixels_white = lightness.GetPixelsUnsafe())
                    {

                        int W = mask.Width;
                        int DupLinesCount = 0;

                        var faskPixelsCount = Math.Min(10, W);
                        if (faskPixelsCount == 0)
                            return new Size(mask.Width, mask.Height);

                        for (int y = 0; y < mask.Height; y++)
                        {
                            MagickColor black = new MagickColor("black");
                            MagickColor white = GetColorBytes(new byte[] { 255 },0,1);

                            bool fastConstentDetectBlack = true;
                            bool fastConstentDetectWhite = true;

                            for (int c = 0; c < faskPixelsCount; c++)
                            {
                                int fastPixelX = (c * W / faskPixelsCount);
                                if (!isPixelSame(black, pixels_mask.GetPixel(fastPixelX, y).ToArray(),4,0))
                                {
                                    fastConstentDetectBlack = false;
                                }
                                if (!isPixelSame(white, pixels_white.GetPixel(fastPixelX, y).ToArray(),1,0))
                                {
                                    fastConstentDetectWhite = false;
                                }
                                if (!fastConstentDetectBlack && !fastConstentDetectWhite)
                                {
                                    break;
                                }
                            }

                            // By tring 10 pixel we guessed, now to really check:
                            if (fastConstentDetectBlack)
                            {
                                bool constantLine = true;
                                byte[] rowPixels = pixels_mask.GetArea(0, y, W, 1);
                                for (int rowI = 0; rowI < rowPixels.Length / 4; rowI++)
                                {
                                    if (!isPixelSame(black, rowPixels,4, 4 * rowI))
                                    {
                                        constantLine = false;
                                        break;
                                    }
                                }
                                if (constantLine) DupLinesCount++;
                            } 
                            else if (fastConstentDetectWhite)
                            {
                                bool constantLine = true;
                                byte[] rowPixels = pixels_white.GetArea(0, y, W, 1);
                                for (int rowI = 0; rowI < rowPixels.Length; rowI++)
                                {
                                    if (!isPixelSame(white, rowPixels,1, rowI))
                                    {
                                        constantLine = false;
                                        break;
                                    }
                                }
                                if (constantLine) DupLinesCount++;
                            }

                        }

                        Console.WriteLine("DUP: " + DupLinesCount);
                        return new Size(mask.Width, mask.Height- DupLinesCount);
                        //clone.LiquidRescale(W, clone.Height - DupLinesCount);
                    }
                }
                
            }
        }
    }
}
