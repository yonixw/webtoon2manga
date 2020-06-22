REM TO C#
REM https://www.imagemagick.org/discourse-server/viewtopic.php?t=33531

SET f=1.png

REM Pencil:
REM convert -size 256x256 xc:  +noise Random  -virtual-pixel tile -motion-blur 0x20+135 -charcoal 1   pencil_tile.gif


REM Get the black 
convert %f% -channel rgba  -fuzz 1%% -fill none +opaque black bg_mask.png
convert bg_mask.png -morphology Erode Disk  -morphology Dilate Disk   bg_finer.png
convert bg_finer.png -transparent white -alpha extract  bg_finer_not.png


REM GOOD:
REM https://www.imagemagick.org/discourse-server/viewtopic.php?t=17736
REM Get none black (fuzz as blur)
REM convert %f% -channel rgba  -alpha set -fuzz 1%% -fill none -opaque black content.png
REM convert %f% -mask bg_finer_not.png content.png
convert %f% -colorspace HSL -channel lightness -separate +channel ( bg_finer_not.png -transparent white ) -compose copy-opacity -composite content.png

REM Patterns
REM https://imagemagick.org/script/formats.php#builtin-patterns
REM CROSSHATCH45

convert bg_finer.png -transparent white -alpha extract -negate ( +clone ( -tile pattern:GRAY95 -resize 500%% ) -draw "color 0,0 reset" +clone +swap -compose color_dodge -composite )  bg_mask_pattern.png

convert -composite bg_mask_pattern-1.png content.png final.png


REM convert %f% -threshold 97%% -morphology dilate octagon -define connected-components:area-threshold=800 -define connected-components:verbose=true -connected-components 8 -auto-level PNG8:lumps.png

REM http://www.imagemagick.org/Usage/resize/#resize
REM http://www.imagemagick.org/Usage/photos/#chroma_key
