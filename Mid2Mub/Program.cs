using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sharktooth.Mub;

namespace Mid2Mub
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputPath, outputPath;

            if (args.Length < 1)
            {
                // Display usage
                Console.WriteLine("Usage: song.mid output.fsgmub");
                return;
            }
            else if (args.Length == 1)
            {
                // Copies arguments
                inputPath = args[0];
                outputPath = ReplaceExtension(inputPath, ".fsgmub");
            }
            else
            {
                inputPath = args[0];
                outputPath = args[1];
            }

            var mubMid = new MubImport(inputPath);
            var mub = mubMid.ExportToMub();
            mub.WriteToFile(outputPath);
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
