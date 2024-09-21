using FoxSky.Img.FileProcessors;
using FoxSky.Img.Service;
using FoxSky.Img.Utilities;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System.Globalization;
using System.Text;

namespace FoxSky.Img.Processors
{
    public class ImageProcessor
    {
        private readonly FileHandler fileHandler;
        private readonly GeolocationService geolocationService;

        public string? PicsOwnerSurname { get; set; }
        public string? SrcPath { get; set; }
        public string? DstRootPath { get; set; }
        public string? UserEmail { get; set; }
        public string? Radius { get; set; }
        public Mode Mode { get; set; }

        public ImageProcessor(FileHandler fileHandler, GeolocationService geolocationService)
        {
            this.fileHandler = fileHandler;
            this.geolocationService = geolocationService;
        }

        public async Task<bool> ProcessImages()
        {
            if (File.Exists(SrcPath))
                return await fileHandler.ProcessImageFile(SrcPath, this);
            else if (System.IO.Directory.Exists(SrcPath))
                return await fileHandler.ProcessDirectory(SrcPath, this);
            else
            {
                Logger.LogError($"{SrcPath} is not a valid file or directory.");
                return false;
            }
        }
    }
}
