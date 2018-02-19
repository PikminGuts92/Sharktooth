using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharktooth.Mub;

namespace Mub2Mid
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputPath, outputPath;

            if (args.Length < 1)
            {
                // Display usage
                Console.WriteLine("Usage: song.fsgmub output.mid");
                return;
            }
            else if (args.Length == 1)
            {
                // Copies arguments
                inputPath = args[0];
                outputPath = ReplaceExtension(inputPath, ".mid");
            }
            else
            {
                inputPath = args[0];
                outputPath = args[1];
            }

            Mub mub = Mub.FromFile(inputPath);
            MubExport mid = new MubExport(mub);
            mid.Export(outputPath);
        }

        static string ReplaceExtension(string path, string extension)
        {
            if (!path.Contains('.'))
                return path + extension;

            int lastIdx = path.LastIndexOf('.');
            return path.Substring(0, lastIdx) + extension;
        }
    }
}
