using System.Reflection;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace x16_png_converter;

public class ConsoleWriter
{
    private readonly Converter conv;

    public ConsoleWriter(Converter converter)
    {
        this.conv = converter;
        //Console.ForegroundColor = Color.White;
        //Console.BackgroundColor = Color.Black;
    }

    public static void PrintHelpText()
    {
        var lines = new string[] {
            "This tool will convert PNG images to a format that the video controller (VERA) of the Commander X16 can read.",
            "Both indexed images (which contain a palette) and full-color images are supported.",
            "The original file can contain an image or a number of tiles or sprites.",
            "For conversion of images, the width of the image must be 320 or 640 pixels. Height has no restrictrions.",
            "For conversion to tiles or sprites, the width and height of each tile/sprite must be specified.",
            "",
            "COLORS",
            "Bits per pixel (BPP) in the generated file will depend on how many colors the conversion results in.",
            "The number of colors might be reduced because the color depth of VERA is limited to 12 bits.",
            "In other words several 32-bit colors in the original image might converted to the same 12-bit color.",
            "Semitransparent colors (0 < alpha < 255) will be treated as solid colors.",
            "",
            "TRANSPARENCY",
            "The first color of the palette might be transparent when rendered by VERA.",
            "This is for example the case when a sprite is rendered in front of a layer.",
            "Therefore it can be absolutely crucial which color in the original image that will receive index 0 in the generated palette.",
            "The selection is made in the following way:",
            "1. If the original image is indexed, the color with index 0 in the original will also receive index 0 in the converted image.",
            "2. If the user has explicitly stated which color should be the first, this color will receive index 0.",
            "3. If nothing above applies, the color of the top left pixel will receive index 0.",
            "",
            "OUTPUT",
            "At least two files will be generated: a binary file with image data and the palette in the specified format.",
            "As an extra bonus a BASIC program that displays the image/tiles/sprites can be generated.",
            "",
            "SYNTAX",
            "                        X16PngConverter [-help] [FILENAME] {-bmx|-image|-tiles|-sprites} [-height] [-width] [-palette] [-transparent] [-demo].",
            "",
            "OPTIONS",
            "(No arguments)        : Displays this text.",
            "",
            "-help/-h              : Same as above if it is the first argument.",
            "",
            "FILENAME              : If the name of the file is the only argument, the original image will be analyzed",
            "                        to see if conversion is possible and in that case which options that are possible.",
            "",
            "-bmx|-image|-tiles|-sprites: Set conversion mode (mandatory). For images, the width must be either 320 or 640 pixels.",
            "                           : -bmx will output a file in the X16 Graphics Format (BMX).",
            "                           : -image will output two files, one file with raw image data, and a file containing the palette (see -palette below).",
            "",
            "-height/-h            : Set height of tiles or sprites (not used when converting to a bitmap image).",
            "                        Valid values for tile mode are 8 and 16, for sprites 8, 16, 32 and 64.",
            "",
            "-width/-w             : Set width of tiles or sprites, (not used when converting to a bitmap image).",
            "                        Valid values are the same as for height.",
            "",
            "-palette/-p           : Set file format for the destination file that contains the palette.",
            "                        Valid values are:",
            "                        bin - a binary file (default).",
            "                        asm - text file containing assembly source code.",
            "                        bas - text file containing BASIC DATA statements.",
            "",
            "-transparent/-t       : Set which color that will have index 0 in the generated palette.",
            "                        The value must be a 32-bit hexadecimal value in the following format:",
            "                        $aarrggbb where a = alpha, r = red, g = green and b = blue.",
            "",
            "-demo/-d              : Generate a demo program in BASIC. This can be loaded to the emulator by using the -bas option.",
            "                        For example: x16emu -bas mysprites_demo.txt. To run it immediately add the option -run.",
            "                        Using this option with other modes than bmx will cause a binary palette file to be created.",
            "",
            "EXAMPLES",
            "X16PngConverter                               : Display this text.",
            "X16PngConverter image.png                     : Analyze image and see if it is possible to convert.",
            "X16PngConverter image.png -bmx                : Convert to the X16 Graphics format (BMX).",
            "X16PngConverter image.png -image              : Convert to a bitmap image.",
            "X16PngConverter image.png -tiles -h 16 -w 16  : Convert image to tiles with a widht and height of 16 pixels.",
            "X16PngConverter image.png -image -p asm       : Convert image to sprites and output palette only as a file with assembly source code.",
            "X16PngConverter image.png -image -t $ff88aacc : Convert image with the specified (potentially transparent) color as the first in the generated palette.",
            "X16PngConverter image.png -image -demo        : Convert image and generate a BASIC demo program named image_demo.txt."
        };

        Console.WriteLine("\n--- X16PngConverter --------------------\n");
        var version = Assembly.GetEntryAssembly().GetName().Version;
        Console.WriteLine($"Version: {version.Major}.{version.Minor}.{version.Build}\r\n");
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }
    }

    public void PrintAnalysis()
    {
        Console.WriteLine();
        PrintInfoAboutOriginal();
        PrintValidOptions();
        Console.WriteLine(); 
        PrintInfoAboutConversion();
    }

    private void PrintValidOptions()
    {
        Console.WriteLine("Valid options:");
        Console.WriteLine("----------------------------------------");
        if (conv.IndexColorsDictionary.Count > 256)
        {
            Console.WriteLine($"FATAL PROBLEM: The image has to be color reduced before conversion, With the current number of colors the conversion would result in {conv.IndexColorsDictionary.Count} colors, maximum is 256.");
        }
        if (conv.Width != 320 && conv.Width != 640)
        {
            Console.WriteLine("Conversion to a bitmap image is not possible because the width is not 320 or 640. (Height has no restrictions.)");
        }
        else
        {
            Console.WriteLine($"Conversion to a bitmap image is possible because the width is {conv.Width}.");
        }

        var sWidths = BuildNumberList(conv.PossibleSpriteWidths);
        var sHeights = BuildNumberList(conv.PossibleSpriteHeights);
        var tWidths = BuildNumberList(conv.PossibleSpriteWidths?.Where(x => x < 32).ToArray());
        var tHeights = BuildNumberList(conv.PossibleSpriteHeights?.Where(x => x < 32).ToArray());

        Console.WriteLine();
        if (sWidths != String.Empty && sHeights != String.Empty)
        {
            Console.WriteLine($"If converting to sprites, valid sizes are:");
            Console.WriteLine($"Width : {sWidths}");
            Console.WriteLine($"Height: {sHeights}");
            Console.WriteLine($"If converting to tiles, valid sizes are:");
            Console.WriteLine($"Width : {tWidths}");
            Console.WriteLine($"Height: {tHeights}");
        }
        else
        {
            Console.WriteLine("The image cannot be converted to tiles or sprites. Width and height must be divisible by 8.");
            Console.WriteLine("This is because sprites can be 8, 16, 32 or 64 pixels wide/high and tiles can be 8 or 16.");
        }
    }

    public void PrintConversionResult(int bytesWritten)
    {
        Console.WriteLine($"\nThe file {conv.FileInfo.FullName} was successfully converted.\n");

        PrintInfoAboutOriginal();
        PrintInfoAboutConversion();

        // File information
        Console.WriteLine("\nFile information:");

        if (conv.Args.Mode == ConversionMode.BMX)
        {
            Console.WriteLine($"{bytesWritten} bytes were written to {conv.FileInfo.BMXImageDataName}.");
            Console.WriteLine("The file format is BMX version 1.0 and the file contains:");
            Console.WriteLine("1. A header of 32 bytes.");
            Console.WriteLine($"2. A palette of {conv.IndexColorsCount} colors (2 bytes each).");
            Console.WriteLine($"3. {conv.SizeInBytes} bytes of image data.");
        }
        else
        {
            Console.WriteLine($"Size of image : {conv.SizeInBytes} bytes (height * width / pixels per byte).");
            Console.WriteLine($"Data written  : {bytesWritten} bytes were written to {conv.FileInfo.RawImageDataName} including a header of 2 bytes.");
            Console.Write($"Colors written: ");
            PrintPaletteFileInfo();
        }

        if (conv.SizeInBytes > 126 * 1024)
        {
            Console.WriteLine($"\nWARNING: The amount of free VRAM is 126 KB, the converted image is larger.");
        }
        if (conv.Args.DemoRequested)
        {
            Console.WriteLine();
            PrintDemoInfo();
        }
    }

    private void PrintInfoAboutOriginal()
    {
        Console.WriteLine("Original image:");
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($"Image width     : {conv.Width}");
        Console.WriteLine($"Image height    : {conv.Height}");
        Console.WriteLine($"Number of colors: {conv.OriginalColorsCount}");
        Console.Write("Color type      : ");
        if (conv.IsIndexed)
        {
            Console.WriteLine($"Indexed (each pixel has a color taken from a limited palette)\n");
        }
        else
        {
            Console.WriteLine("Full-color (each pixel has its own ARGB value)\n");
        }
    }

    private void PrintInfoAboutConversion()
    {
        Console.WriteLine("Conversion:");
        Console.WriteLine("----------------------------------------");

        var modeLowerWord = conv.Args.Mode.ToString().ToLower();
        var modeSingularWord = WordInSingular(modeLowerWord);
        var modeCapitalSingularWord = FirstToUpper(modeSingularWord);
        if (conv.Args.Mode == ConversionMode.Tiles || conv.Args.Mode == ConversionMode.Sprites)
        {
            Console.WriteLine($"{modeCapitalSingularWord} width       : {conv.Args.Width}");
            Console.WriteLine($"{modeCapitalSingularWord} height      : {conv.Args.Height}");
            Console.WriteLine($"Number of {modeLowerWord}  : {(conv.Height / conv.Args.Height) * (conv.Width / conv.Args.Width)}");
            Console.WriteLine($"Size of each {modeSingularWord}: {conv.Args.Height * conv.Args.Width / conv.ColMode.PixelsPerByte} bytes\n");
        }
        if (conv.IndexColorsDictionary.Count <= 256) {
            Console.WriteLine($"Number of colors     : {conv.IndexColorsDictionary.Count}");
            Console.WriteLine($"Bits per pixel (BPP) : {conv.ColMode.BitsPerPixel}");
            Console.WriteLine($"Pixels per byte      : {conv.ColMode.PixelsPerByte}");
            Console.WriteLine($"Color depth          : {conv.ColMode.ColorDepth}\n");
        }
        PrintColorList();
    
        // Tile order
        if (conv.Args.Mode == ConversionMode.Tiles || conv.Args.Mode == ConversionMode.Sprites)
        {
            Console.WriteLine($"The {modeLowerWord} were read from the original image in the following order:\n");
            PrintTileTable();
        }
    }

    private void PrintPaletteFileInfo()
    {
        if (conv.Args.FileFormat != PaletteFileFormat.Binary && conv.Args.DemoRequested)
        {
            Console.Write($"{conv.IndexColorsDictionary.Count} colors were written to { conv.FileInfo.BinPaletteName}. ");
            return;
        }
        if (conv.Args.Mode == ConversionMode.BMX)
        {
            return;
        }
        switch (conv.Args.FileFormat)
        {
            case PaletteFileFormat.Binary:
                Console.WriteLine($"{conv.IndexColorsDictionary.Count} colors were written to { conv.FileInfo.BinPaletteName}.");
                break;
            case PaletteFileFormat.Assembler:
                Console.WriteLine($"{conv.IndexColorsDictionary.Count} colors were written to {conv.FileInfo.AsmPaletteName}.");
                break;
            case PaletteFileFormat.BASIC:
                Console.WriteLine($"{conv.IndexColorsDictionary.Count} colors were written to {conv.FileInfo.BasicPaletteName}");
                break;
            default:
                Console.WriteLine($"{conv.IndexColorsDictionary.Count} colors were written to { conv.FileInfo.BinPaletteName}, { conv.FileInfo.BasicPaletteName} and { conv.FileInfo.AsmPaletteName}.");
                break;
        }
    }

    private void PrintDemoInfo()
    {
        Console.WriteLine("\nDemo:");
        if (conv.SizeInBytes > 110 * 1024)
        {
            Console.WriteLine("NOTE: No demo program in BASIC has been created, a size of 110 KB is maximum for this.");
            return;
        }
        if (conv.Args.Mode == ConversionMode.BMX && conv.Width == 640)
        {
            Console.WriteLine("NOTE: No demo program in BASIC has been created, only BMX images with a width of 320 pixels are supported.");
            return;
        }
        Console.WriteLine($"The program {conv.FileInfo.BasicProgramName} is a simple BASIC program to display the {conv.Args.Mode.ToString().ToLower()}.");
        Console.WriteLine($"Start the emulator with \"x16emu -bas {conv.FileInfo.BasicProgramName} -run\" to load and run it.");
    }

        private void PrintTileTable()
    {
        var rowCount = conv.Height / conv.Args.Height;
        var colCount = conv.Width / conv.Args.Width;
        int row;
        for (row = 0; row < rowCount && row * colCount + colCount <= 1000; row++)
        {
            PrintTileRow(row * colCount, colCount);
        }
        PrintLastTileRow(colCount);
        if (row < rowCount)
        {
            PrintToBeContinuedTileRow(colCount);
            PrintToBeContinuedTileRow(colCount);
        }
    }

    private void PrintTileRow(int startIndex, int colCount)
    {
        var sb1 = new StringBuilder();
        var sb2 = new StringBuilder();
        var i = startIndex;
        for (var col = 0; col < colCount; col++)
        {
            if (col == 16)
            {
                sb1.Append("|     ");
                sb2.Append("| ... ");
                i += colCount - 1 - col;
                col = colCount - 1;
            }
            sb1.Append("|-----");
            sb2.Append($"| {i,3} ");
            i++;
        }
        sb1.Append('|');
        sb2.Append('|');
        Console.WriteLine(sb1.ToString());
        Console.WriteLine(sb2.ToString());
    }

    private static void PrintLastTileRow(int colCount)
    {
        var sb = new StringBuilder();
        int col;
        for (col = 0; col < colCount && col < 16; col++)
        {
            sb.Append("|-----");
        }
        if (colCount > 16)
        {
            sb.Append("|     |-----");
        }
        sb.Append('|');
        Console.WriteLine(sb.ToString());
    }

    private static void PrintToBeContinuedTileRow(int colCount)
    {
        var sb = new StringBuilder();
        for (var col = 0; col < colCount; col++)
        {
            if (col == 16)
            {
                sb.Append("      ");
                col = colCount - 2;
                continue;
            }
            sb.Append("   .  ");
        }
        Console.WriteLine(sb.ToString());
    }

    public void PrintColorList()
    {
        if (conv.IndexColorsDictionary.Count > 16)
        {
            PrintLongColorList();
            return;
        }
        Console.WriteLine("Palette:");
        Console.WriteLine("Index  VERA colors  Original colors");
        //var index = 0;
        foreach (var item in conv.IndexColorsDictionary.OrderBy(key => key.Value))
        {
            Console.Write($"{item.Value,5}  {item.Key} ");
            Console.Write("     ");
            Console.Write("  ");
            var originalColors = conv.OriginalColorsDictionary[item.Key];
            for (var i = 0; i < originalColors.Count - 1; i++)
            {
                Console.Write($"{RGBAToHex(originalColors[i])}, ");
            }
            Console.WriteLine($"{RGBAToHex(originalColors[^1])}");
        }
    }

    private void PrintLongColorList()
    {
        if (conv.IndexColorsDictionary.Count > 256 || conv.OriginalColorsCount > 2048)
        {
            return;
        }
        Console.WriteLine("Palette:");
        Console.WriteLine("At the top of each column is the 12-bit VERA color, below corresponding color(s) in the original image.");
        Console.WriteLine("----------------------------------------");
        var index = 0;
        var colorList = conv.IndexColorsDictionary.OrderBy(item => item.Value).Select(item => item.Key).ToList();
        foreach (var veraColor in colorList)
        {
            if (index % 16 == 0)
            {
                var indexRowStop = index + 15 < colorList.Count ? index + 15 : colorList.Count - 1;
                Console.Write($"{index}-{indexRowStop} ".PadLeft(8));
            }
            Console.Write($"  {veraColor}   ");
            index++;
            if (index % 16 == 0)
            {
                Console.WriteLine();
                PrintOriginalColors(index - 16, 16, colorList);
            }
        }
        if (index <= conv.IndexColorsDictionary.Count)
        {
            Console.WriteLine();
            var colCount = index % 16;
            PrintOriginalColors(index - colCount, colCount,  colorList);
        } 
    }

    private void PrintOriginalColors(int offset, int colCount, List<VERAColor> colorList)
    {
        var row = 0;
        bool colorPrinted;
        //Console.ForegroundColor = Color.LightGreen;
        do
        {
            colorPrinted = false;
            Console.Write("        ");
            for (var col = 0; col < colCount; col++)
            {
                var argb = conv.OriginalColorsDictionary[colorList[col + offset]].ElementAtOrDefault(row);
                if (argb != null)
                {
                    colorPrinted = true;
                    Console.Write($"{RGBAToHex(argb)} ");
                    continue;
                }
                Console.Write(".         ");
            }
            Console.WriteLine();
            row++;
        }
        while (colorPrinted);
        //Console.ForegroundColor = Color.White;
    }

    private static string BuildNumberList(int[] list)
    {
        if (list.Length == 0)
        {
            return String.Empty;
        }
        var sb = new StringBuilder();
        foreach (var value in list)
        {
            sb.AppendFormat("{0}, ", value.ToString());
        }
        if (sb.Length > 1)
        {
            sb.Remove(sb.Length - 2, 2);
        }
        return sb.ToString();
    }

    public static string RGBAToHex(uint? rgba)
    {
        if (rgba == null)
        {
            return String.Empty;
        }
        byte r = (byte)((rgba >> 24) & 0xFF);
        byte g = (byte)((rgba >> 16) & 0xFF);
        byte b = (byte)((rgba >> 8) & 0xFF);
        byte a = (byte)(rgba & 0xFF);

        var color = Color.FromRgba(r, g, b, a);
        var pixel = color.ToPixel<Rgba32>();

        return string.Format("${0:X2}{1:X2}{2:X2}{3:X2}", pixel.A, pixel.R, pixel.G, pixel.B);
    }

    private static string WordInSingular(string word)
    {
        return word.Substring(word.Length - 1, 1) == "s" ? word[0..^1] : word;
    } 

    private static string FirstToUpper(string word)
    {
        return word[..1].ToUpper() + word[1..];
    }
}
