using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Xmk2Mid
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<XmkOptions>(args)
                .WithParsed(op =>
                {
                    var parser = new XmkOptionsParser();

                    try
                    {
                        parser.Parse(op);
                        Console.WriteLine($"Wrote output to \"{op.OutputPath}\"");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                });
        }
    }
}
