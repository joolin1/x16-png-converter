namespace x16_png_converter;

public class ConversionFileInfo
{
    public ConversionFileInfo(string filename)
    {
        Path = System.IO.Path.GetDirectoryName(filename);
        Name = System.IO.Path.GetFileNameWithoutExtension(filename);
        FullName = System.IO.Path.GetFileName(filename);
        RawImageDataName = $"{Name.ToUpper()}.BIN";
        BMXImageDataName = $"{Name.ToUpper()}.BMX";
        BinPaletteName = $"{Name.ToUpper()}-PALETTE.BIN";
        AsmPaletteName = $"{Name}_palette.asm";
        BasicPaletteName = $"{Name}_BASIC_palette.txt";
        BasicProgramName = $"{Name}_demo.txt";
    }

    public readonly string Path;
    public readonly string Name;
    public readonly string FullName;
    public readonly string RawImageDataName;
    public readonly string BMXImageDataName;
    public readonly string BinPaletteName;
    public readonly string AsmPaletteName;
    public readonly string BasicPaletteName;
    public readonly string BasicProgramName;
}

