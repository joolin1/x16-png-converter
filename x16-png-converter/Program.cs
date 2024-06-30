using SixLabors.ImageSharp;
using System.ComponentModel;

namespace x16_png_converter;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var arguments = new ConversionArguments(args);

            if (arguments.Filename == null)
            {
                ConsoleWriter.PrintHelpText();
                return;
            }

            //using var srcImage = (Image<Rgba32>)Image<Rgba32>.Load(arguments.Filename);
            using var srcImage = Image.Load(arguments.Filename);
            var conv = new Converter(srcImage, arguments);
            var consoleWriter = new ConsoleWriter(conv);

            if (arguments.Mode == ConversionMode.NotSet)
            {
                consoleWriter.PrintAnalysis();
                return;
            }
            var bytesWritten = conv.Convert();
            consoleWriter.PrintConversionResult(bytesWritten);
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"ERROR: The file {ex.Message} is not found.");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        catch (BadImageFormatException ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        catch (UnknownImageFormatException ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        catch (Win32Exception ex)
        {
            Console.WriteLine($"ERROR STARTING EMULATOR: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);  
        }
    }
}


