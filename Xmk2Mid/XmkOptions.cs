using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace Xmk2Mid
{
    public class XmkOptions
    {
        [Value(0, HelpText = "Input path(s) for single FAR archive or multiple XMK files.", Required = true)]
        public IEnumerable<string> InputPaths { get; set; }

        [Option('q', "quantization", HelpText = "MIDI quantization value for correcting note positions, must be ratio or decimal between 0 and 1 (use 0 to disable).", Default = "1/128")]
        public string Quantization { get; set; }

        [Option('r', "remap", HelpText = "Remap output MIDI tracks for CH/RB use.")]
        public bool RemapForCH { get; set; }

        [Option('o', "output", HelpText = "Path to output MIDI.", Required = true)]
        public string OutputPath { get; set; }

        [Usage(ApplicationAlias = "Xmk2Mid.exe")]
        public static IEnumerable<Example> Examples
            => new []
            {
                new Example("Convert FAR archive to MIDI",
                    new XmkOptions
                    {
                        InputPaths = new []
                        {
                            "GHL2517.far"
                        },
                        OutputPath = "GHL2517.mid"
                    }),
                new Example("Convert FAR archive to MIDI with remapping",
                    new XmkOptions
                    {
                        InputPaths = new []
                        {
                            "GHL2517.far"
                        },
                        OutputPath = "GHL2517.mid",
                        RemapForCH = true
                    }),
                new Example("Convert FAR archive to MIDI using 1/32nd quantization",
                    new XmkOptions
                    {
                        InputPaths = new []
                        {
                            "GHL2517.far"
                        },
                        OutputPath = "GHL2517.mid",
                        Quantization = "1/32"
                    }),
                new Example("Convert XMK files to MIDI -- Warning: Output MIDI will use tempo map from first XMK file",
                    new XmkOptions
                    {
                        InputPaths = new []
                        {
                            "touchdrums.xmk",
                            "touchguitar.xmk",
                            "guitar_3x2.xmk",
                            "vocals.xmk",
                            "control.xmk"
                        },
                        OutputPath = "./GHL2517.mid"
                    })
            };
    }
}
