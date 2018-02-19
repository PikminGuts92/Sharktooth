using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Sharktooth;
using Sharktooth.Xmk;

namespace Xmk2Mid
{
    class Program
    {
        static void Main(string[] args)
        {
            string inputPath, outputPath;

            if (args.Length < 1)
            {
                // Display usage
                Console.WriteLine("Usage: GHL2517.far output.mid");
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


            if (inputPath.EndsWith(".far", StringComparison.CurrentCultureIgnoreCase))
            {
                // Extracts xmk files from archive
                FarArchive archive = new FarArchive(inputPath);
                var files = archive.GetAllFilesByExtension(".xmk");
                List<Xmk> xmks = new List<Xmk>();
                
                // Reads in xmk files
                foreach (FarEntry file in files)
                {
                    using (var stream = new MemoryStream(file.GetBytes()))
                    {
                        Xmk xmk = Xmk.FromStream(stream, file.Name);
                        xmks.Add(xmk);
                    }
                }

                // Converts XMK files
                XmkExport mid = new XmkExport(xmks);
                mid.Export(outputPath, true);

                archive.Dispose();
            }
            else if (inputPath.EndsWith(".xmk", StringComparison.CurrentCultureIgnoreCase))
            {
                // Directly converts XMK file
                Xmk xmk = Xmk.FromFile(inputPath);
                XmkExport mid = new XmkExport(xmk);
                mid.Export(outputPath);
            }
            else
                Console.WriteLine($"\"{Path.GetExtension(inputPath)}\" extension is unsupported. Must be either .xmk or .far");
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
