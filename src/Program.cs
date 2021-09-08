using System;
using System.IO;
using CommandLine;
using MGF;

namespace MGF_Packer
{
    class Program
    {
        public class Options
        {
            [Value(0, Required = true, HelpText = "Which game to package this MGF file for (ma1 = MechAssault 1, ma2 = MechAssault 2)")]
            public string Game { get; set; }

            [Value(1, Required = true, HelpText = "Source folder to package")]
            public string SourceFolder { get; set; }

            [Option('n', "name", Required = false, HelpText = "Name of MGF file (defaults to source folder name if unset)")]
            public string Name { get; set; }
        }

        static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (!(o.Game == "ma1" || o.Game == "ma2"))
                    {
                        throw new Exception("First argument must be \"ma1\" or \"ma2\".");
                    }

                    if (!Directory.Exists(o.SourceFolder))
                    {
                        throw new Exception("Source folder path does not exist.");
                    }

                    string destination = Path.Combine(o.SourceFolder, Path.GetFileNameWithoutExtension(o.Name ?? Path.GetFileNameWithoutExtension(o.SourceFolder)) + ".mgf");

                    Run(o.SourceFolder, destination, o.Game == "ma1" ? MGF.Version.MechAssault : MGF.Version.MechAssault2LW);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
        }

        static void Run(string folder, string dest, MGF.Version game)
        {
            if (game == MGF.Version.MechAssault)
            {
                Console.WriteLine($"Packing {folder} to {dest} for MechAssault");
                var packer = new Packer<FileRecordMA1>(folder);
                packer.WriteToFile(dest);
            }
            else
            {
                Console.WriteLine($"Packing {folder} to {dest} for MechAssault 2: Lone Wolf");
                var packer = new Packer<FileRecordMA2>(folder);
                packer.WriteToFile(dest);
            }
        }
    }
}
