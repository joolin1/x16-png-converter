using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.Text;
using SixLabors.ImageSharp.PixelFormats;

namespace x16_png_converter;

public class Converter
{
    public ConversionArguments Args { get; }
    public ConversionFileInfo FileInfo { get; }
    public int Width { get; }
    public int Height { get; }
    public bool IsIndexed { get; }
    public PngColorType? ColorType { get; }
    public ColorMode ColMode { get; }
    public int SizeInBytes { get; set; }
    public int[]? PossibleSpriteWidths { get; }
    public int[]? PossibleSpriteHeights { get; }
    public Dictionary<VERAColor, byte> IndexColorsDictionary { get; } = [];
    public Dictionary<VERAColor, List<uint?>> OriginalColorsDictionary { get; } = [];
    public int OriginalColorsCount { get { return OriginalColorsDictionary.Keys.Sum(key => OriginalColorsDictionary[key].Count); } }
    public int IndexColorsCount { get { return IndexColorsDictionary.Count; } }
    private PngBitDepth? BitDepth { get; set; }
    private Color[] Palette { get; set; }
    private Image<Rgba32>? SrcImage { get; set; }

    public Converter(Image srcImage, ConversionArguments args)
    {
        Args = args;
        if (!srcImage.Metadata.TryGetPngMetadata(out var metaData))
        {
            throw new BadImageFormatException("The image format is not png.");
        }

        ColorType = metaData.ColorType;
        SrcImage = ColorType == PngColorType.RgbWithAlpha ? (Image<Rgba32>)srcImage : srcImage.CloneAs<Rgba32>();
        BitDepth = metaData.BitDepth;
        Width = srcImage.Width;
        Height = srcImage.Height;
        if (metaData.ColorType == PngColorType.Palette)
        {
            IsIndexed = true;
            Palette = metaData.ColorTable.Value.ToArray();
            CreateColorDictionariesFromPalette();
        }
        else
        {
            IsIndexed = false;
            CreateColorDictionariesFromBitmap();
        }
        PossibleSpriteWidths = CheckPossibleSizes(Width);
        PossibleSpriteHeights = CheckPossibleSizes(Height);
        ColMode = new ColorMode(IndexColorsDictionary.Count, Args.Mode);
        FileInfo = new ConversionFileInfo(args.Filename);

    }

    private static int[] CheckPossibleSizes(int size)
    {
        var sizes = Array.Empty<int>();
        if (size % 64 == 0)
        {
            sizes = [8, 16, 32, 64];
        }
        else if (size % 32 == 0)
        {
            sizes = [8, 16, 32];
        }
        else if (size % 16 == 0)
        {
            sizes = [8, 16];
        }
        else if (size % 8 == 0)
        {
            sizes = [8];
        }
        return sizes;
    }

    private void CheckIfConversionPossible()
    {
        if (IndexColorsDictionary.Count > 256)
        {
            throw new BadImageFormatException($"The image has {OriginalColorsCount} colors. A conversion would result in {IndexColorsDictionary.Count} colors. Maximum is 256.");
        }
        if (Args.Mode != ConversionMode.Image && Args.Mode != ConversionMode.BMX)
        {
            CheckSize();
            return;
        }
        if (Width != 320 && Width != 640)
        {
            throw new BadImageFormatException($"The width of the image is {Width}, it must be either 320 or 640. (Height has no restrictions.)");
        }
    }

    private void CheckSize()
    {
        if (!PossibleSpriteWidths.Contains(Args.Width))
        {
            throw new BadImageFormatException($"The width of the image is not divisible by {Args.Width}.");
        }
        if (!PossibleSpriteHeights.Contains(Args.Height))
        {
            throw new BadImageFormatException($"The height of the image is not divisible by {Args.Height}.");
        }
    } 

    public int Convert()
    {
        CheckIfConversionPossible();
        SizeInBytes = Height * Width / ColMode.PixelsPerByte;
        if (Args.Mode != ConversionMode.BMX) {
            WritePaletteFile();
            if (SizeInBytes <= 110 * 1024 && Args.DemoRequested)
            {
                WriteBasicProgramFile(Path.Combine(FileInfo.Path, FileInfo.BasicProgramName));
            }
        }
        else
        {
            if(Width == 320 && Args.DemoRequested)
            {
                WriteBasicProgramFile(Path.Combine(FileInfo.Path, FileInfo.BasicProgramName));
            }
        }
        var filename = Args.Mode == ConversionMode.BMX ? FileInfo.BMXImageDataName : FileInfo.RawImageDataName;
        return WriteBitmapFile(Path.Combine(FileInfo.Path, filename));
    }

    private void WritePaletteFile()
    {
        var binaryPaletteWritten = false;

        switch (Args.FileFormat)
        {
            case PaletteFileFormat.NotSet:
                if (Args.Mode == ConversionMode.BMX)
                {
                    break;
                }
                WriteBinaryPaletteFile(Path.Combine(FileInfo.Path, FileInfo.BinPaletteName));
                binaryPaletteWritten = true;
                break;
            case PaletteFileFormat.Binary:
                WriteBinaryPaletteFile(Path.Combine(FileInfo.Path, FileInfo.BinPaletteName));
                binaryPaletteWritten = true;
                break;
            case PaletteFileFormat.BASIC:
                WriteTextPaletteFile(Path.Combine(FileInfo.Path, FileInfo.BasicPaletteName));
                break;
            case PaletteFileFormat.Assembler:
                WriteTextPaletteFile(Path.Combine(FileInfo.Path, FileInfo.AsmPaletteName));
                break;
        }
        if (Args.DemoRequested && !binaryPaletteWritten)
        {
            WriteBinaryPaletteFile(Path.Combine(FileInfo.Path, FileInfo.BinPaletteName));
        }
    }

    private void CreateColorDictionariesFromPalette()
    {
        SelectTransparentColorFromPalette();

        var index = 0;
        
        foreach (var color in Palette)
        {
            index += AddToColorDictionaries(color, index) ? 1 : 0;
        }
    }

    private void SelectTransparentColorFromPalette()
    {
        // First check if user specified a color
        if (Args.TransparentColor == null)
        {
            return;
        }
        foreach (var color in Palette)
        {
            if (color.ToHex() == Args.TransparentColor)
            {
                SetTransparentColor(color);
                return;
            }
        }
        throw new ArgumentException($"The specified transparent color {Args.TransparentColor} was not found in the image.");
    }

    private void CreateColorDictionariesFromBitmap()
    {
        SelectTransparentColorFromBitmap();
        var index = 1;
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var color = SrcImage[x, y];
                index += AddToColorDictionaries(color, index) ? 1 : 0;
            }
        }
    }

    private bool AddToColorDictionaries(Color color, int index)
    {
        var veraColor = new VERAColor(color);
        var rgba = color.ToPixel<Rgba32>().Rgba;
        if (!IndexColorsDictionary.ContainsKey(veraColor))
        {
            IndexColorsDictionary.Add(veraColor, (byte)index);
            OriginalColorsDictionary.Add(veraColor, [rgba]);
            return true;
        }
        if (!OriginalColorsDictionary[veraColor].Contains(rgba))
        {
            OriginalColorsDictionary[veraColor].Add(rgba);
        }
        return false;
    }

private void SelectTransparentColorFromBitmap() // Set which color should have index 0 in the palette and be potentially transparent
    {
        // First check if user specified a color
        if (Args.TransparentColor != null)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var color = SrcImage[x, y];
                    if (color.ToHex() == Args.TransparentColor)
                    {
                        SetTransparentColor(color);
                        return;
                    }
                }
            }
            //throw new ArgumentException($"The specified transparent color {ConsoleWriter.ARGBToHex((uint?)Args.TransparentColor)} was not found in the image.");
            throw new ArgumentException($"The specified transparent color {Args.TransparentColor} was not found in the image.");
        }
        // Then check if there is any transparent color in the image
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var color = SrcImage[x, y];
                if (color.A == 0)
                {
                    SetTransparentColor(color);
                    return;
                }
            }
        }
        // final alternative, take the color of the top left pixel
        SetTransparentColor(SrcImage[0, 0]);
    }

    private void SetTransparentColor(Color color)
    {
        var veraColor = new VERAColor(color);
        IndexColorsDictionary.Add(veraColor, 0);
        //OriginalColorsDictionary.Add(color.ToArgb(), veraColor);
        OriginalColorsDictionary.Add(veraColor, [color.ToPixel<Rgba32>().Rgba]);
    }

    private int WriteBMXHeader(BinaryWriter writer)
    {
        byte bitDepth = BitConverter.GetBytes(ColMode.BitsPerPixel)[0];
        byte VERAcolorDepth = BitConverter.GetBytes(ColMode.ColorDepth)[0];
        byte[] width16 = BitConverter.GetBytes(Width);
        byte[] height16 = BitConverter.GetBytes(Height);
        byte palUsed = IndexColorsCount == 256 ? (byte)0 : BitConverter.GetBytes(IndexColorsCount)[0];
        byte[] imgOffset = BitConverter.GetBytes(IndexColorsCount * 2 + 32); 

        var bmxHeader = new byte[] {
            0x42, 0x4d, 0x58,           // header
            0x01,                       // version
            bitDepth,                   // color depth
            VERAcolorDepth,             // VERA Color Depth Register
            width16[0], width16[1],     // width
            height16[0], height16[1],   // height
            palUsed, 0,                 // number of colors, start palette index is 0
            imgOffset[0], imgOffset[1], // offset in file where image data starts
            0, 0,                       // no compression, no VERA border color
            0, 0, 0, 0, 0, 0, 0, 0,     // unused
            0, 0, 0, 0, 0, 0, 0, 0      // unused
        };
        writer.Write(bmxHeader);
        return bmxHeader.Length;
    }

    private int WriteBitmapFile(string destFilename)
    {
        int byteCount = 0;
        using var writer = new BinaryWriter(new FileStream(destFilename, FileMode.Create, FileAccess.Write));

        if (Args.Mode == ConversionMode.BMX)
        {
            byteCount = WriteBMXHeader(writer);
            foreach (var item in IndexColorsDictionary.OrderBy(key => key.Value))
            {
                writer.Write(item.Key.ToBinaryValues()); // write palette
                byteCount += 2;
            }
        }
        else
        {
            writer.Write(new byte[] { 0, 0 }); // add a two byte header
            byteCount = 2;
        }
        var tileWidth = Args.Width != 0 ? Args.Width : Width; // Set tile width to width of image if image conversion
        int tileHeight = Args.Height != 0 ? Args.Height : Height;

        var rowCount = Height / tileHeight;
        var colCount = Width / tileWidth;
        for (var row = 0; row < rowCount; row++) // loop through rows of tiles/sprites
        {
            for (var col = 0; col < colCount; col++) // loop through columns of tiles/sprites
            {
                for (var y = 0; y < tileHeight; y++) // loop through rows of pixels in tile/sprite
                {
                    for (var x = 0; x < tileWidth; x += ColMode.PixelsPerByte) // loop through columns of pixels in tile/sprite
                    {
                        var byteValue = 0;
                        var shift = 8;
                        for (var xx = 0; xx < ColMode.PixelsPerByte; xx++) // loop through pixels in same destination byte
                        {
                            var color = SrcImage[col * tileWidth + x + xx, row * tileHeight + y];
                            var veraColor = new VERAColor(color);
                            shift -= ColMode.BitsPerPixel;
                            var index = color.A == 0 ? 0 : IndexColorsDictionary[veraColor]; // if color transparent set index to 0 otherwise look up index in dictionary
                            byteValue += (byte)(index << shift);
                        }
                        writer.Write((byte)byteValue);
                        byteCount++;
                    }
                }
            }
        }
        writer.Close();
        return byteCount;
    }

    //private int WriteBitmapFile(string destFilename)
    //{
    //    using var writer = new BinaryWriter(new FileStream(destFilename, FileMode.Create, FileAccess.Write));
    //    writer.Write(new byte[] { 0, 0 }); // add a two byte header
    //    var byteCount = 2;
    //    var tileWidth = Args.Width != 0 ? Args.Width : Width; // Set tile width to width of image if image conversion
    //    int tileHeight = Args.Height != 0 ? Args.Height : Height;

    //    var rowCount = Height / tileHeight;
    //    var colCount = Width / tileWidth;
    //    for (var row = 0; row < rowCount; row++) // loop through rows of tiles/sprites
    //    {
    //        for (var col = 0; col < colCount; col++) // loop through columns of tiles/sprites
    //        {
    //            for (var y = 0; y < tileHeight; y++) // loop through rows of pixels in tile/sprite
    //            {
    //                for (var x = 0; x < tileWidth; x += ColMode.PixelsPerByte) // loop through columns of pixels in tile/sprite
    //                {
    //                    var byteValue = 0;
    //                    var shift = 8;
    //                    for (var xx = 0; xx < ColMode.PixelsPerByte; xx++) // loop through pixels in same destination byte
    //                    {
    //                        var color = Bitmap.GetPixel(col * tileWidth + x + xx, row * tileHeight + y);
    //                        var veraColor = new VERAColor(color);
    //                        shift -= ColMode.BitsPerPixel;
    //                        var index = color.A == 0 ? 0 : IndexColorsDictionary[veraColor]; // if color transparent set index to 0 otherwise look up index in dictionary
    //                        byteValue += (byte)(index << shift);
    //                    }
    //                    writer.Write((byte)byteValue);
    //                    byteCount++;
    //                }
    //            }
    //        }
    //    }
    //    writer.Close();
    //    return byteCount;
    //}

    private void WriteBasicProgramFile(string filename)
    {
        using var writer = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write));
        var sb = new StringBuilder();
        var name = Path.GetFileNameWithoutExtension(Args.Filename).ToUpper();
        string basicProgram = "Something went wrong when generating this file.";

        switch (Args.Mode)
        {
            case ConversionMode.BMX:
                if (Width == 320)
                {
                    basicProgram = String.Join(Environment.NewLine,
                    $"100 REM *** LOAD IMAGE TO BANKED RAM ***",
                    $"110 REM",
                    $"120 BLOAD \"{name}.BMX\", 8, 1,$A000",
                    $"130 BANK 1:REM SET BANK TO BEGINNING OF DATA",
                    $"140 CD = PEEK($A005): REM GET VERA COLOR DEPTH (0-3)",
                    $"150 CL = PEEK($A00A): REM GET NUMBER OF COLORS",
                    $"160 DL = PEEK($A00C): REM GET START OF IMAGE LO BYTE",
                    $"170 DH = PEEK($A00D): REM GET START OF IMAGE HI BYTE",
                    $"180 OF = DH*256+DL  : REM CALCULATE IMAGE DATA OFFSET",
                    $"190 REM",
                    $"200 REM *** SET PALETTE ***",
                    $"210 REM",
                    $"220 IF CL=0 THEN CL=256",
                    $"230 FOR I=0 TO CL*2-1",
                    $"240 VPOKE 1,$FA00+I, PEEK($A020+I)",
                    $"250 NEXT",
                    $"260 REM",
                    $"270 REM *** LOAD IMAGE AGAIN TO VRAM ***",
                    $"280 REM",
                    $"290 BVLOAD \"{name}.BMX\", 8, 0,$800-OF",
                    $"300 SCREEN 3:REM SET SCREEN TO 320X240",
                    $"310 POKE $9F29, 16+1:REM SWITCH TO LAYER 0, VGA OUTPUT",
                    $"320 POKE $9F2D, 4+CD:REM BITMAP MODE, BPP={ColMode.BitsPerPixel}",
                    $"330 POKE $9F2F, 4:REM IMAGE BASE=$800, WIDTH=320",
                    $"340 GET A$:IF A$=\"\" THEN 340",
                    $"350 REM",
                    $"360 REM *** RESTORE SCREEN ***",
                    $"370 REM",
                    $"380 COLOR 1, 6:REM FG=WHITE, BG=BLUE",
                    $"390 POKE $9F29, 32+1: REM SWITCH BACK TO LAYER 1, VGA OUTPUT",
                    $"400 REM",
                    $"410 REM *** RESTORE FIRST 16 COLORS ***",
                    $"420 REM",
                    $"430 FOR I = 0 TO 31",
                    $"440 READ C",
                    $"450 VPOKE 1,$FA00+I, C",
                    $"460 NEXT",
                    $"470 SCREEN 0:CLS",
                    $"480 DATA $00,$00,$FF,$0F,$00,$08,$FE,$0A,$4C,$0C,$C5,$00,$0A,$00,$E7,$0E",
                    $"490 DATA $85,$0D,$40,$06,$77,$0F,$33,$03,$77,$07,$F6,$0A,$8F,$00,$BB,$0B");
                }
                else
                {
                    basicProgram = "Sorry, BASIC demo programs can only be generated for images that are 320 pixels wide.";
                }
                break;
            case ConversionMode.Image:
                if (Width == 320)
                {
                    basicProgram = String.Join(Environment.NewLine,
                        $"100 REM LOAD BINARY FILES",
                        $"110 VLOAD \"{name}.BIN\",8,0,$4000",
                        $"120 VLOAD \"{name}-PALETTE.BIN\",8,1,$FA00",
                        $"130 REM",
                        $"140 REM SETUP SCREEN",
                        $"150 SCREEN 3:REM SET SCREEN TO 320X240",
                        $"160 POKE $9F29,16+1:REM SWITCH TO LAYER 0, VGA OUTPUT",
                        $"170 POKE $9F2D,4+{ColMode.ColorDepth}:REM BITMAP MODE, BPP={ColMode.BitsPerPixel}",
                        $"180 POKE $9F2F,32+0:REM TILE (IMAGE) BASE=$4000, BITMAP WIDTH=320",
                        $"190 GET A$:IF A$=\"\" THEN 190",
                        $"200 REM",
                        $"210 REM RESTORE SCREEN",
                        $"220 COLOR 1,6:REM FG=WHITE, BG=BLUE",
                        $"230 POKE $9F29,32+1: REM SWITCH BACK TO LAYER 1, VGA OUTPUT",
                        $"240 REM",
                        $"250 REM RESTORE CHAR SET IN CASE IT HAS BEEN OVERWRITTEN",
                        $"260 PRINT CHR$($8E):REM SET UPPER CASE WHICH CAUSES UPLOAD OF CHAR SET",
                        $"270 REM",
                        $"280 REM RESTORE FIRST 16 COLORS",
                        $"290 FOR I = 0 TO 31",
                        $"300 READ C",
                        $"310 VPOKE 1,$FA00+I,C",
                        $"320 NEXT",
                        $"330 SCREEN 0:CLS",
                        $"340 DATA $00,$00,$FF,$0F,$00,$08,$FE,$0A,$4C,$0C,$C5,$00,$0A,$00,$E7,$0E",
                        $"350 DATA $85,$0D,$40,$06,$77,$0F,$33,$03,$77,$07,$F6,$0A,$8F,$00,$BB,$0B");
                } else
                {
                    basicProgram = String.Join(Environment.NewLine,
                        $"100 REM LOAD BINARY FILES",
                        $"110 VLOAD \"{name}.BIN\",8,0,$4000",
                        $"120 VLOAD \"{name}-PALETTE.BIN\",8,1,$FA00",
                        $"130 REM",
                        $"140 REM SETUP SCREEN",
                        $"150 POKE $9F29,16+1:REM SWITCH TO LAYER 0, VGA OUTPUT",
                        $"160 POKE $9F2D,4+{ColMode.ColorDepth}:REM BITMAP MODE, BPP={ColMode.BitsPerPixel}",
                        $"170 POKE $9F2F,32+1:REM IMAGE BASE=$4000, BITMAP WIDTH=640",
                        $"180 GET A$:IF A$=\"\" THEN 180",
                        $"190 REM",
                        $"200 REM RESTORE SCREEN",
                        $"210 COLOR 1,6:REM FG=WHITE, BG=BLUE",
                        $"220 POKE $9F29,32+1: REM SWITCH BACK TO LAYER 1, VGA OUTPUT",
                        $"230 REM",
                        $"240 REM RESTORE CHAR SET IN CASE IT HAS BEEN OVERWRITTEN",
                        $"250 PRINT CHR$($8E):REM SET UPPER CASE WHICH CAUSES UPLOAD OF CHAR SET",
                        $"260 REM",
                        $"270 REM RESTORE FIRST 16 COLORS",
                        $"280 FOR I = 0 TO 31",
                        $"290 READ C",
                        $"300 VPOKE 1,$FA00+I,C",
                        $"310 NEXT",
                        $"320 CLS",
                        $"330 DATA $00,$00,$FF,$0F,$00,$08,$FE,$0A,$4C,$0C,$C5,$00,$0A,$00,$E7,$0E",
                        $"340 DATA $85,$0D,$40,$06,$77,$0F,$33,$03,$77,$07,$F6,$0A,$8F,$00,$BB,$0B");
                }
                break;
            case ConversionMode.Tiles:
                var tileLastIndex = (Height / Args.Height) * (Width / Args.Width) - 1;
                var tileSizeBits = (Args.Height == 16 ? 2 : 0) + (Args.Width == 16 ? 1 : 0);
                basicProgram = String.Join(Environment.NewLine,
                    $"100 REM LOAD BINARY FILES",
                    $"110 VLOAD \"{name}.BIN\",8,0,$4000",
                    $"120 VLOAD \"{name}-PALETTE.BIN\",8,1,$FA00",
                    $"130 REM",
                    $"140 REM SETUP SCREEN",
                    $"150 SCREEN 3:REM SET SCREEN TO 320X240",
                    $"160 POKE $9F29,16+1:REM SWITCH TO LAYER 0, VGA OUTPUT",
                    $"170 POKE $9F2D,{ColMode.ColorDepth}:REM MAP HEIGHT=32, MAP WIDTH=32, BPP={ColMode.BitsPerPixel}",
                    $"180 POKE $9F2E,16+8:REM MAP BASE=$3000",
                    $"190 POKE $9F2F,32+{tileSizeBits}:REM TILE BASE=$4000, TILE HEIGHT={Args.Height}, TILE WIDTH={Args.Height}",
                    $"200 REM",
                    $"210 REM CREATE MAP",
                    $"220 I=0",
                    $"230 FOR ROW=0 TO 31",
                    $"240 FOR COL=0 TO 31",
                    $"250 ADDR=$3000+ROW*64+COL*2",
                    $"260 VPOKE 0,ADDR,I:REM TILE INDEX",
                    $"270 VPOKE 0,ADDR+1,0:REM PALETTE INDEX 0, NO FLIPS",
                    $"280 I=I+1:IF I>{tileLastIndex} THEN I=0",
                    $"290 NEXT COL",
                    $"300 NEXT ROW",
                    $"310 GET A$:IF A$=\"\" THEN 310",
                    $"320 REM",
                    $"330 REM RESTORE SCREEN",
                    $"340 SCREEN 0:REM SET SCREEN TO 640X480",
                    $"350 COLOR 1,6:REM FG = WHITE, BG = BLUE",
                    $"360 POKE $9F29,32+1: REM SWITCH BACK TO LAYER 1, VGA OUTPUT",
                    $"370 REM",
                    $"380 REM RESTORE CHAR SET IN CASE IT HAS BEEN OVERWRITTEN",
                    $"390 PRINT CHR$($8E):REM SET UPPER CASE WHICH CAUSES UPLOAD OF CHAR SET",
                    $"400 REM",
                    $"410 REM RESTORE FIRST 16 COLORS",
                    $"420 FOR I = 0 TO 31",
                    $"430 READ C",
                    $"440 VPOKE 1,$FA00+I,C",
                    $"450 NEXT",
                    $"460 CLS",
                    $"470 REM ORIGINAL PALETTE",
                    $"480 DATA $00,$00,$FF,$0F,$00,$08,$FE,$0A,$4C,$0C,$C5,$00,$0A,$00,$E7,$0E",
                    $"490 DATA $85,$0D,$40,$06,$77,$0F,$33,$03,$77,$07,$F6,$0A,$8F,$00,$BB,$0B");
                break;
            case ConversionMode.Sprites:
                var paletteHexOffset = ColMode.ColorCount == 256 ? "00" : "20";
                var paletteIndex = ColMode.ColorCount == 256 ? "0" : "1";
                var spriteHeightBits = System.Convert.ToString((int)(Math.Log2(Args.Height / 8)), 2).PadLeft(2, '0');
                var spriteWidthBits = System.Convert.ToString((int)(Math.Log2(Args.Width / 8)), 2).PadLeft(2, '0');
                var spriteSizeInBytes = Args.Height * Args.Width / ColMode.PixelsPerByte;
                var spriteLastIndex = (Height / Args.Height) * (Width / Args.Width) - 1;

                basicProgram = String.Join(Environment.NewLine,
                    $"100 REM LOAD BINARY FILES",
                    $"110 ADDR=$4000",
                    $"120 VLOAD \"{name}.BIN\",8,0,ADDR",
                    $"130 VLOAD \"{name}-PALETTE.BIN\",8,1,$FA{paletteHexOffset}:REM TO PALETTE OFFSET {paletteIndex}",
                    $"140 REM",
                    $"150 REM SETUP SCREEN",
                    $"160 SCREEN 3:REM SET SCREEN TO 320X240",
                    $"170 POKE $9F29,PEEK($9F29) OR %01000000:REM ENABLE SPRITES",
                    $"180 REM",
                    $"190 REM SET UP SPRITES",
                    $"200 REG=$FC00",
                    $"210 X=0:Y=0",
                    $"220 FOR I=0 TO {spriteLastIndex}:REM LOOP THROUGH ALL SPRITES",
                    $"230 VPOKE 1,REG,ADDR/32 AND 255:REM ADDRESS BITS 12:5",
                    $"240 VPOKE 1,REG+1,0+ADDR/8192:REM COLOR MODE + ADDRESS BITS 16:13",
                    $"250 VPOKE 1,REG+2,X AND 255",
                    $"260 VPOKE 1,REG+3,X/256",
                    $"270 VPOKE 1,REG+4,Y AND 255",
                    $"280 VPOKE 1,REG+5,Y/256",
                    $"290 VPOKE 1,REG+6,%00001100:REM SET Z-DEPTH",
                    $"300 VPOKE 1,REG+7,%{spriteHeightBits}{spriteWidthBits}0001:REM HEIGHT={Args.Height}, WIDTH={Args.Width}, PALETTE OFFSET=1",
                    $"310 REG=REG+8",
                    $"320 ADDR=ADDR+{spriteSizeInBytes}:REM ADD SIZE OF SPRITE IN BYTES",
                    $"330 X=X+{Args.Width}",
                    $"340 IF X=320 THEN Y=Y+{Args.Height}",
                    $"350 IF X=320 THEN X=0",
                    $"360 IF Y>=240-{Args.Height} THEN 380",
                    $"370 NEXT",
                    $"380 GET A$:IF A$=\"\" THEN 380",
                    $"390 REM",
                    $"400 REM RESTORE SCREEN",
                    $"410 POKE $9F29,PEEK($9F29) AND %10111111:REM DISABLE SPRITES",
                    $"420 SCREEN 0:REM SET SCREEN TO 640X480",
                    $"430 REM RESTORE ORIGINAL CHAR SET THAT MIGHT HAVE BEEN OVERWRITTEN",
                    $"440 PRINT CHR$($8E):REM SET UPPER CASE (CAUSES UPLOAD OF CHAR SET)");
                break;
        }
        writer.WriteLine(basicProgram);
        writer.Close();
    }

    private void WriteBinaryPaletteFile(string filename)
    {
        using var writer = new BinaryWriter(new FileStream(filename, FileMode.Create, FileAccess.Write));
        writer.Write(new byte[] { 0, 0 }); // add a two byte header
        foreach (var item in IndexColorsDictionary.OrderBy(key => key.Value))
        {
            writer.Write(item.Key.ToBinaryValues());
        }
        writer.Close();
    }

    private void WriteTextPaletteFile(string filename)
    {
        var ext = Path.GetExtension(filename);
        var sb = new StringBuilder();
        var colorCount = 0;
        var index = 0;
        foreach (var item in IndexColorsDictionary.OrderBy(key => key.Value))
        {
            sb.Append(ext == ".asm" ? GetAsmColorString(item.Key, index++) : GetDATAColorString(item.Key, index++));
            colorCount++;
        }

        using var writer = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write));
        writer.WriteLine(sb.ToString());
        writer.Close();
    }

    private static string GetAsmColorString(VERAColor veraColor, int index)
    {
        if (index == 0)
        {
            return $".word {veraColor}";
        }
        if (index % 16 == 0)
        {
            return $"\r\n.word {veraColor}";
        }
        return $", {veraColor}";
    }

    private static string GetDATAColorString(VERAColor veraColor, int index)
    {
        var startRow = 1000;
        if (index == 0)
        {
            return $"{startRow} DATA {veraColor.ToBasicData()}";
        }
        if (index % 8 == 0)
        {
            return $"\r\n{startRow + 10 * index / 8} DATA {veraColor.ToBasicData()}";
        }
        return $",{veraColor.ToBasicData()}";
    }
}

