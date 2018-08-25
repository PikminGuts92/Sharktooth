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
            if (args.Length < 1)
            {
                // Display usage
                Console.WriteLine("Usage: GHL2517[.far|.xmk]");
                return;
            }

            var farArgs = args.Where(x => x.EndsWith(".far", StringComparison.CurrentCultureIgnoreCase)).ToList();
            var xmkArgs = args.Where(x => x.EndsWith(".xmk", StringComparison.CurrentCultureIgnoreCase)).ToList();

            foreach (var farPath in farArgs)
            {
                // Extracts xmk files from archive
                FarArchive archive;

                try
                {
                    archive = new FarArchive(farPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to open \"{farPath}\" because \"{ex.Message}\"");
                    return;
                }

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

                ConvertXmks(xmks, ReplaceExtension(farPath, ".mid"));
                archive.Dispose();
            }

            if (xmkArgs.Count > 0)
            {
                List<Xmk> xmks = new List<Xmk>();
                string outputPath = xmkArgs.Count == 1 ?
                    ReplaceExtension(xmkArgs[0], ".mid") :
                    Path.Combine(Path.GetDirectoryName(xmkArgs.First()), "combinedFromXmks.mid");

                // Reads in xmk files
                foreach (var xmkPath in xmkArgs)
                {
                    using (var stream = File.OpenRead(xmkPath))
                    {
                        Xmk xmk = Xmk.FromStream(stream, Path.GetFileName(xmkPath));
                        xmks.Add(xmk);
                    }
                }

                // Uses directory of first xmk file
                ConvertXmks(xmks, Path.Combine(Path.GetDirectoryName(xmkArgs.First()), outputPath));
            }
        }

        static void ConvertXmks(List<Xmk> xmks, string outputPath)
        {
            // Converts XMK files
            XmkExport mid = new XmkExport(xmks);
            mid.Export(outputPath, true);
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
