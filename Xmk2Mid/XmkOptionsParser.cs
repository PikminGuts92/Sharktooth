using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sharktooth;
using Sharktooth.Xmk;
using Xmk2Mid.Exceptions;

namespace Xmk2Mid
{
    public class XmkOptionsParser
    {
        private readonly Regex _farExtRegex = new Regex("(?i).far$");
        private readonly Regex _xmkExtRegex = new Regex("(?i).xmk$");

        private readonly Regex _fractionRegex = new Regex(@"^(\d+)/(\d+)$");
        private readonly Regex _decimalRegex = new Regex(@"^(\d*)(([.](\d+))?)$"); // TODO: Revisit as it allows empty input

        public virtual void Parse(XmkOptions op)
        {
            ValidateInputPaths(op.InputPaths);
            // TODO: Verify paths exist? Should already be handled by .NET

            var expOptions = new XmkExportOptions
            {
                Quantization = ParseQuantization(op.Quantization),
                Remap = op.RemapForCH
            };

            var xmks = GetXmks(op.InputPaths);
            ConvertXmks(xmks, expOptions, op.OutputPath);
        }

        public virtual void ValidateInputPaths(IEnumerable<string> paths)
        {
            var farPaths = GetFarPaths(paths);
            var xmkPaths = GetXmkPaths(paths);

            var invalidPaths = paths
                .Except(farPaths)
                .Except(xmkPaths)
                .ToList();

            if (invalidPaths.Count > 0)
            {
                throw new UnsupportedInputException(invalidPaths.First());
            }

            if (farPaths.Count > 1)
            {
                throw new MultipleFarInputException();
            }

            if (farPaths.Count >= 1 && xmkPaths.Count >= 1)
            {
                throw new XmkAndFarInputMixedException();
            }
        }

        public virtual decimal ParseQuantization(string quan)
        {
            if (IsFraction(quan))
            {
                var frac = _fractionRegex.Match(quan);
                return decimal.Parse(frac.Groups[1].Value) / decimal.Parse(frac.Groups[2].Value);
            }
            else if (IsDecimal(quan))
            {
                return decimal.Parse(quan);
            }

            throw new QuantizationInvalidException(quan);
        }

        public virtual bool IsFraction(string v) => _fractionRegex.IsMatch(v);
        public virtual bool IsDecimal(string v) => _decimalRegex.IsMatch(v);

        public virtual IList<string> GetFarPaths(IEnumerable<string> paths)
            => paths
                .Where(x => _farExtRegex.IsMatch(x))
                .ToList();

        public virtual IList<string> GetXmkPaths(IEnumerable<string> paths)
            => paths
                .Where(x => _xmkExtRegex.IsMatch(x))
                .ToList();

        public virtual IList<Xmk> GetXmks(IEnumerable<string> paths)
        {
            var farPath = GetFarPaths(paths)
                .SingleOrDefault();

            // Parse single far archive
            if (!(farPath is null))
            {
                return GetXmksFromFarPath(farPath);
            }

            // Parse multiple xmk files
            var xmkPaths = GetXmkPaths(paths);
            return GetXmksFromXmkPaths(xmkPaths);
        }

        public virtual IList<Xmk> GetXmksFromFarPath(string farPath)
        {
            // Gets xmk files from archive
            FarArchive archive;

            try
            {
                archive = new FarArchive(farPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to open \"{farPath}\" because \"{ex.Message}\"");
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

            // Dispose archive and return xmks
            archive.Dispose();
            return xmks;
        }

        public virtual IList<Xmk> GetXmksFromXmkPaths(IList<string> xmkPaths)
        {
            List<Xmk> xmks = new List<Xmk>();

            // Reads in xmk files
            foreach (var xmkPath in xmkPaths)
            {
                using (var stream = File.OpenRead(xmkPath))
                {
                    Xmk xmk = Xmk.FromStream(stream, Path.GetFileName(xmkPath));
                    xmks.Add(xmk);
                }
            }

            return xmks;
        }

        public virtual void ConvertXmks(IList<Xmk> xmks, XmkExportOptions expOps, string outputPath)
        {
            // Converts XMK files
            XmkExport mid = new XmkExport(xmks, expOps);
            mid.Export(outputPath);
        }
    }
}
