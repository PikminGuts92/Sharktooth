using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameArchives;
using GameArchives.FSAR;

namespace Sharktooth
{
    public class FarArchive : IDisposable
    {
        private string _archivePath;
        private FSARPackage _archive;

        public FarArchive(string path)
        {
            _archivePath = path;

            try
            {
                AbstractPackage pkg = PackageReader.ReadPackageFromFile(path);

                if (pkg is FSARPackage)
                    _archive = pkg as FSARPackage;
            }
            catch
            {
                // Do something?
            }
        }

        public List<FarEntry> GetAllFilesByExtension(string ext)
        {
            List<FarEntry> files = new List<FarEntry>();

            void ParseDirectory(IDirectory directory)
            {
                // Checks each file in current directory
                foreach (IFile file in directory.Files)
                {
                    if (file.Name.EndsWith(ext, StringComparison.CurrentCultureIgnoreCase))
                        files.Add(new FarEntry(file));
                }

                // Searches deeper
                foreach (IDirectory dir in directory.Dirs)
                    ParseDirectory(dir);
            }

            ParseDirectory(_archive.RootDirectory);
            return files;
        }

        public void Dispose()
        {
            _archive.Dispose();
        }

        public bool IsOpen => _archive != null;
    }
}
