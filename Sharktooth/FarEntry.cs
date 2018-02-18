using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameArchives;
using System.IO;

namespace Sharktooth
{
    // Basically just wraps IFile
    public class FarEntry
    {
        private readonly IFile _file;

        internal FarEntry(IFile file)
        {
            _file = file;
        }

        public string Name => _file.Name;
        public long Size => _file.Size;
        public long CompressedSize => _file.CompressedSize;
        public Stream Stream => _file.Stream;

        public byte[] GetBytes() => _file.GetBytes();
        public Stream GetStream() => _file.GetStream();

        public override string ToString() => _file.Name;
    }
}
