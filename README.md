# X16PngConverter

This is a console application for Windows, macOS and Linux that will convert PNG images to a format that the video controller (VERA) of the Commander X16 can read. Images can also be converted to the X16 Graphics Format (BMX). Both indexed images (which contain a palette) and full-color images are supported. The original file can contain an image or several tiles or sprites with max 256 colors. For conversion of images, the width of the image must be 320 or 640 pixels. Height has no restrictions. For conversion to tiles or sprites, the width and height of each tile/sprite must be specified.

## Colors
Bits per pixel (BPP) in the generated file will depend on how many colors the conversion results in. The number of colors might be reduced because the color depth of VERA is limited to 12 bits. In other words, several 32-bit colors in the original image might be converted to the same 12-bit color. Semitransparent colors (alpha < 255) will be treated as solid colors.

## Transparency
The first color of the palette might be transparent when rendered by VERA. For example, this is when a sprite is rendered in front of a layer. Therefore it can be crucial which color in the original image will receive index 0 in the generated palette. The selection is made in the following way:
If the original image is indexed (has a palette), the color with index 0 in the original will also receive index 0 in the converted image.
If the user has explicitly stated which color should be the first, this color will receive index 0.
If nothing above applies, the color of the top left pixel will receive index 0.

## Output
At least two files will be generated: a binary file with image data and the palette in binary format or the format of assembly source code or BASIC. As a bonus a BASIC program that displays the image/tiles/sprites can be generated.

## Installation
Download the latest release, it includes versions for Windows, macOS and Linux. No installation is needed, there is just one executable file for each system.

## Syntax
X16PngConverter [-help] [FILENAME] {-bmx|-image|-tiles|-sprites} [-height] [-width] [-palette] [-transparent] [-demo].

## Options
| Argument                    |                     |
| --------------------------- | ------------------- |
| (No arguments)              | Displays this text. |
| -help/-h                    | Same as above if it is the first argument. |
| FILENAME                    | If the filename is the only argument, the original image will be analyzed to see if conversion is possible and if so which options are possible. |
| -bmx                        | Set conversion mode. Convert to a file in the X16 Graphics Format (BMX). |
| -image                      | Set conversion mode. Convert to the native format of VERA. This will result in one file with raw image data, and another containing the palette (see -palette below). |
| -tiles/-sprites             | Set conversion mode. Interpret the image as consisting of several tiles or sprites. Convert them to the native format of VERA. This will result in one file with raw tile/sprite data, and another containing the palette (see -palette below). |
| -height/-h                  | Set the height of tiles or sprites (not used when converting to a bitmap image). Valid values for tile mode are 8 and 16, for sprites 8, 16, 32, and 64. |
| -width/-w                   | Set the width of tiles or sprites, (not used when converting to a bitmap image). Valid values are the same as for height. |
| -palette/-p                 | Set file format for the destination file that contains the palette. Valid values are: |
|                             | bin - a binary file (the default). |
|                             | asm - text file containing assembly source code). |
|                             | bas - text file containing BASIC DATA statements. |
| -transparent/-t             | Set which color will have index 0 in the generated palette. The value must be a 32-bit hexadecimal value in the following format: $AARRGGBB where A = alpha, R = red, G = green, and B = blue. |
| -demo/-d                    | Generate a demo program in BASIC. This can be loaded to the emulator by using the -bas option. For example: x16emu -bas mysprites_demo.txt. To run it immediately add the option -run. Using this option will cause a binary palette file to be created. |

## Examples
| Command                                       |                    |
| --------------------------------------------- | ------------------ |
| X16PngConverter                               | Display help text. |
| X16PngConverter image.png                     | Analyse the image and see if conversion is possible. |
| X16PngConverter image.png -bmx                | Convert to the X16 Graphics format (BMX). |
| X16PngConverter image.png -image              | Convert to a bitmap image (width must be 320 or 640 pixels). |
| X16PngConverter image.png -tiles -h 16 -w 16  | Convert to tiles with a width and height of 16 pixels. |
| X16PngConverter image.png -image -p asm       | Convert to sprites and output palette only as a file with assembly source code. |
| X16PngConverter image.png -image -t $ff88aacc | Convert the image with the specified (potentially transparent) color as the first in the generated palette. |
| X16PngConverter image.png -image -demo        | Convert the image and generate a BASIC demo program named image_demo.txt. |
