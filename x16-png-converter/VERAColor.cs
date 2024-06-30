using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace x16_png_converter;

public class VERAColor
{
    public VERAColor(Color color)
    {
        var rgba = color.ToPixel<Rgba32>();
        a = rgba.A;
        r = rgba.R;
        g = rgba.G; 
        b = rgba.B;
        A = rgba.A > 0 ? 15 : 0; // All semi transparencies is set to solid
        R = To4BitValue(rgba.R);
        G = To4BitValue(rgba.G);
        B = To4BitValue(rgba.B);
    }

    //public VERAColor(uint argb32) : this(Color.FromArgb(argb32)) // takes a 32 bit ARGB value
    //{
    //}

    public override string ToString()
    {
        return $"$0{R:X}{G:X}{B:X}";
    }

    public string ToBasicData()
    {
        return $"${G:X}{B:X},$0{R:X}";
    }

    public byte[] ToBinaryValues()
    {
        return [(byte)((G << 4) + B), (byte)R];
    }

    public override bool Equals(object? obj)
    {
        var other = (VERAColor)obj;
        return A == other.A && R == other.R && G == other.G && B == other.B;
    }

    public override int GetHashCode()
    {
        return (A << 24) + (R << 16) + (G << 8) + B;
    }

    public Color ToSystemColor()
    {
        return Color.FromRgba((byte)r, (byte)g, (byte)b, (byte)a);
    }

    private static int To4BitValue(int value)
    {
        value = (value + 8) / 16;
        value = value > 15 ? 15 : value;
        return value;
    }

    public readonly int A, R, G, B; // 4 bit values
    private readonly int a, r, g , b; // 8 bit values
}

