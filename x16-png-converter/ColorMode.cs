namespace x16_png_converter;
public class ColorMode
{
    public readonly int BitsPerPixel;   
    public readonly int PixelsPerByte; 
    public readonly int ColorCount;
    public readonly int ColorDepth;
    private readonly Dictionary<int, int> spriteColorDepthDictionary = new() { { 16, 0 }, { 256, 1 } };
    private readonly Dictionary<int, int> colorDepthDictionary = new() { { 2, 0 }, { 4, 1 }, { 16, 2 }, { 256, 3 } };

    public ColorMode(int colorCount, ConversionMode mode)
    {
        if (colorCount <0)
        {
            throw new ArgumentException("Color count cannot be a negative number.");
        }

        int[] validCounts = mode == ConversionMode.Sprites ? [16, 256] : [2, 4, 16, 256];

        foreach (var validCount in validCounts)
        {
            if (validCount >= colorCount )
            {
                ColorCount = validCount;
                break;
            }
        }
        if (ColorCount == 0)
        {
            return;
        }

        BitsPerPixel = (int)Math.Log2(ColorCount);
        PixelsPerByte = 8 / BitsPerPixel;
        if (mode == ConversionMode.Sprites)
        {
            ColorDepth = spriteColorDepthDictionary[ColorCount];
            return;
        }
        ColorDepth = colorDepthDictionary[ColorCount];
    }

}

