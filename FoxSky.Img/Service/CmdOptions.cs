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

        [Option('g', "geolocation",/* Required = true,*/ HelpText = "Enable including geolocation in new file name.")]
        public bool GeolocationFlag { get; set; } = false;

        [Option('e', "email", Required = false, HelpText = "OSM user email address.")]
        public string UserEmail { get; set; } = string.Empty;

        [Option('r', "radius", Required = false, HelpText = "Search radius in meters.")]
        public string Radius { get; set; }

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
