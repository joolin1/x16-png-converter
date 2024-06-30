# X16PngConverter

This is a console application for Windows, MaxOS and Linux that will convert png images to a format that the video controller (VERA) of the Commander X16 can read. Images can also be converted to the X16 Graphics Format (BMX). Both indexed images (which contain a palette) and full-color images are supported. The original file can contain an image or a number of tiles or sprites with max 256 colors. For conversion of images, the width of the image must be 320 or 640 pixels. Height has no restrictrions. For conversion to tiles or sprites, the width and height of each tile/sprite must be specified.

## Colors
Bits per pixel (BPP) in the generated file will depend on how many colors the conversion results in. The number of colors might be reduced because the color depth of VERA is limited to 12 bits. In other words several 32-bit colors in the original image might be converted to the same 12-bit color. Semitransparent colors (alpha < 255) will be treated as solid colors.

## Transparency
The first color of the palette might be transparent when rendered by VERA. This is for example the case when a sprite is rendered in front of a layer. Therefore it can be absolutely crucial which color in the original image that will receive index 0 in the generated palette. The selection is made in the following way:
If the original image is indexed (has a palette), the color with index 0 in the original will also receive index 0 in the converted image.
If the user has explicitly stated which color should be the first, this color will receive index 0.
If nothing above applies, the color of the top left pixel will receive index 0.

## Output
At least two files will be generated: a binary file with image data and the palette in binary format or in the format of assembly source code or BASIC. As an extra bonus a BASIC program that displays the image/tiles/sprites can be generated.

## Installation
Download the right release for your system (Windows, MacOS or Linux). No installation is needed, there is just one executable file.

## Syntax
X16PngConverter [-help] [FILENAME] {-bmx|-image|-tiles|-sprites} [-height] [-width] [-palette] [-transparent] [-demo].

## Options
(No arguments)              : Displays this text.
-help/-h                    : Same as above if it is the first argument.
FILENAME                    : If the name of the file is the only argument, the original image will be analyzed to see if conversion is possible and in that case which options that are possible.
-bmx|-image|-tiles|-sprites : Set conversion mode (mandatory). For images, the width must be either 320 or 640 pixels.
    -bmx will output a file in the X16 Graphics Format (BMX).
    -image will output two files, one file with raw image data, and a file containing the palette (see -palette below).
    -height/-h : Set height of tiles or sprites (not used when converting to a bitmap image). Valid values for tile mode are 8 and 16, for sprites 8, 16, 32 and 64.
    -width/-w : Set width of tiles or sprites, (not used when converting to a bitmap image). Valid values are the same as for height.
-palette/-p                 : Set file format for the destination file that contains the palette. Valid values are:
    bin - a binary file (the default)
    asm - text file containing assembly source code)
    bas - text file containing BASIC DATA statements).
-transparent/-t             : Set which color that will have index 0 in the generated palette. The value must be a 32-bit hexadecimal value in the following format: $AARRGGBB where A = alpha, R = red, G = green and B = blue.
-demo/-d                    : Generate a demo program in BASIC. This can be loaded to the emulator by using the -bas option. For example: x16emu -bas mysprites_demo.txt. To run it immediately add the option -run. Using this option will cause a binary palette file to be created.

## Examples
X16PngConverter                               : Display help text.
X16PngConverter image.png                     : Analyse image and see if it is possible to convert.
X16PngConverter image.png -bmx                : Convert to the X16 Graphics format (BMX).
X16PngConverter image.png -image              : Convert to a bitmap image (width must be 320 or 640 pixels).
X16PngConverter image.png -tiles -h 16 -w 16  : Convert to tiles with a width and height of 16 pixels.
X16PngConverter image.png -image -p asm       : Convert to sprites and output palette only as a file with assembly source code.
X16PngConverter image.png -image -t $ff88aacc : Convert image with the specified (potentially transparent) color as the first in the generated palette.
X16PngConverter image.png -image -demo        : Convert image and generate a BASIC demo program named image_demo.txt.
