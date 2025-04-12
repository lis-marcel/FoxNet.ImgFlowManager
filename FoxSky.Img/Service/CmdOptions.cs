using CommandLine;
using FoxSky.Img.FileProcessors;
using FoxSky.Img.Utilities;

namespace FoxSky.Img.Service
{
    public enum OperationMode
    {
        Copy,
        Move
    }

    public class CmdOptions
    {
        [Option('o', "owner", Required = true, HelpText = "Pictures owner surname.")]
        public string OwnerSurname { get; set; } = string.Empty;

        [Option('s', "source", Required = true, HelpText = "Source directory path.")]
        public string SrcPath { get; set; } = string.Empty;

        [Option('d', "destination", Required = true, HelpText = "Destination directory path.")]
        public string DstPath { get; set; } = string.Empty;

        [Option('m', "mode", Required = false, HelpText = "Select operation mode: Copy or Move. Copy is set as primary mode.")]
        public OperationMode Mode { get; set; } = OperationMode.Copy;

        public int ValidateAndPrompt()
        {
            if (string.IsNullOrEmpty(OwnerSurname) || string.IsNullOrEmpty(SrcPath) || string.IsNullOrEmpty(DstPath))
            {
                Logger.LogError("Requred fields can't be empty.");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }

            return (int)EnviromentExitCodes.ExitCodes.Succcess;
        }
    }
}
