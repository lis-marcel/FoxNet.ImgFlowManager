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
        [Option('s', "source", Required = true, HelpText = "Provide source path")]
        public string SrcPath { get; set; } = string.Empty;

        [Option('d', "destination", Required = true, HelpText = "Provide destination path")]
        public string DstPath { get; set; } = string.Empty;

        [Option('m', "mode", Required = false, HelpText = "Select operation mode: Copy or Move. Copy is set as initial mode.")]
        public OperationMode Mode { get; set; } = OperationMode.Copy;

        public int ValidateAndPrompt()
        {
            if (string.IsNullOrEmpty(SrcPath) || string.IsNullOrEmpty(DstPath))
            {
                Logger.LogError("Source and destination path can't be empty.");
                return (int)EnviromentExitCodes.ExitCodes.Error;
            }

            return (int)EnviromentExitCodes.ExitCodes.Succcess;
        }
    }
}
