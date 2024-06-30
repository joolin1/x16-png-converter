using System.Text.RegularExpressions;

namespace x16_png_converter;

public enum ConversionMode
{
    NotSet, Image, BMX, Tiles, Sprites
}

public enum PaletteFileFormat
{
    NotSet, Binary, BASIC, Assembler
}

public class ConversionArguments
{
    private readonly string[] args;
    private string WordForMode { get { return Mode == ConversionMode.Tiles ? "tile" : "sprite"; } }
    public readonly string? Filename;
    public ConversionMode Mode { get; set; } = ConversionMode.NotSet;
    public int Width { get; set; }
    public int Height { get; set; }
    public string TransparentColor { get; set; }
    public PaletteFileFormat FileFormat { get; set; } = PaletteFileFormat.NotSet;
    public bool DemoRequested { get; set; } = false;

    public ConversionArguments(string[] args)
    {
        this.args = args;
        if (args == null || args.Length == 0)
        {
            Filename = null;
            return;
        }

        if (args[0].ToLower() == "-help" || args[0].ToLower() == "-h")
        {
            Filename = null;
            return;
        }
        Filename = args[0];

        if (!File.Exists(Filename))
        {
            throw new FileNotFoundException(Filename);
        }

        if (args.Length == 1)
        {
            return;
        }

        SetMode();
        SetDetails();

        if (Mode == ConversionMode.Image || Mode == ConversionMode.BMX)
        {
            return;
        }

        CheckSize(Width, "Width");
        CheckSize(Height, "Height");
    }

    private void SetDetails()
    {
        for (var i = 1; i < args.Length; i++)
        {
            i = args[i].ToLower() switch
            {
                "-width" or "-w" => SetWidth(i),
                "-height" or "-h" => SetHeight(i),
                "-transparent" or "-t" => SetTransparentColor(i),
                "-palette" or "-p" => SetPaletteFileFormat(i),
                "-demo" or "-d" => SetDemoRequested(i),
                "-image" or "-bmx" or "-tiles" or "-sprites" => i,
                _ => throw new ArgumentException($"The argument {args[i]} is not valid."),
            };
        }
    }

    private void SetMode()
    {
        foreach (var arg in args)
        {
            switch (arg.ToLower())
            {
                case "-image":
                    Mode = ConversionMode.Image;
                    break;
                case "-bmx":
                    Mode = ConversionMode.BMX;
                    break;
                case "-tiles":
                    Mode = ConversionMode.Tiles;
                    break;
                case "-sprites":
                    Mode = ConversionMode.Sprites;
                    break;
            }
        }
        if (Mode == ConversionMode.NotSet)
        {
            throw new ArgumentException("Conversion mode is not set. Use -image, -bmx, -tiles or -sprites.");
        }
    }

    private int SetWidth(int i)
    {
        if (Mode == ConversionMode.Image || Mode == ConversionMode.BMX)
        {
            throw new ArgumentException("Width should not be set when converting an image.");
        }
        try
        {
            Width = int.Parse(args[++i]);
        }
        catch (IndexOutOfRangeException)
        {
            throw new ArgumentException($"There is no value for {WordForMode} width.");
        }
        catch (Exception)
        {
            throw new ArgumentException($"$The value {args[i]} for {WordForMode} width is not valid.");
        }
        return i;
    }

    private int SetHeight(int i)
    {
        if (Mode == ConversionMode.Image || Mode == ConversionMode.BMX)
        {
            throw new ArgumentException("Height should not be set when converting an image.");
        }
        try
        {
            Height = int.Parse(args[++i]);
        }
        catch (IndexOutOfRangeException)
        {
            throw new ArgumentException($"There is no value for {WordForMode} height.");
        }
        catch (Exception)
        {
            throw new ArgumentException($"The value {args[i]} for {WordForMode} height is not valid.");
        }
        return i;
    }

    private int SetTransparentColor(int i)
    {
        try
        {
            var match = Regex.Match(args[++i].ToLower(), "^\\$(?<value>[\\dabcdef]{8})$");
            if (!match.Success)
            {
                throw new ArgumentException($"The value {args[i]} for transparent color is not valid. It should be a 32 bit hexadecimal number with format $aarrggbb.");
            }
            var value = match.Groups["value"];
            TransparentColor = value.ToString().ToUpper();
            //TransparentColor = Convert.ToInt32(value.ToString(), 16);
            return i;
        }
        catch (IndexOutOfRangeException)
        {
            throw new ArgumentException("There is no value for palette file format.");
        }
    }

    private int SetPaletteFileFormat(int i)
    {
        try
        {
            var match = Regex.Match(args[++i].ToLower(), "^bin|bas|asm$");
            if (!match.Success)
            {
                throw new ArgumentException($"The value {args[i]} for palette file format is not valid. It should be \"bin\", \"bas\" or \"asm\".");
            }
            switch (match.Value.ToString())
            {
                case "bin":
                    FileFormat = PaletteFileFormat.Binary;
                    break;
                case "bas":
                    FileFormat = PaletteFileFormat.BASIC;
                    break;
                case "asm":
                    FileFormat = PaletteFileFormat.Assembler;
                    break;
            }
            return i;
        }
        catch (IndexOutOfRangeException)
        {
            throw new ArgumentException("There is no value for palette file format.");
        }        
    }

    private int SetDemoRequested(int i)
    {
        DemoRequested = true;
        return i;
    }

    private void CheckSize(int size, string word)
    {
        var tileSizes = new int[] { 8, 16 };
        var spriteSizes = new int[] { 8, 16, 32, 64};
        if (Mode == ConversionMode.Tiles)
        {
            if (size == 0)
            {
                throw new ArgumentException($"{word} is not specified.");
            }
            if (!tileSizes.Contains(size))
            {
                throw new ArgumentException($"The value for {word.ToLower()} must be 8 or 16 when converting tiles.");
            }
        }
        if (Mode == ConversionMode.Sprites) {
            if (!spriteSizes.Contains(size)) {
                throw new ArgumentException($"The value for {word.ToLower()} must be 8, 16, 32 or 64 when converting sprites.");
            }
        }
    }

}

