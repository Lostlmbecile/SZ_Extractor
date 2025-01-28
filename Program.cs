using CommandLine;

namespace SZ_Extractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(options =>
                {
                    try
                    {
                        options.Validate();
                        Extractor extractor = new(options);
                        if (!String.IsNullOrEmpty(options.ContentPath))
                        {
                            extractor.Run();
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        Console.Error.WriteLine($"Error: {ex.Message}");
                        Environment.Exit(1);
                    }
                });
        }
    }
}